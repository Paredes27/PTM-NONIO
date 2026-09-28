using System.Security.Claims;
using MentoriaAPI.Controllers;
using MentoriaAPI.Data;
using MentoriaAPI.Dtos;
using MentoriaAPI.Models;
using MentoriaAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var dbPath = Path.Combine(Path.GetTempPath(), $"mentoria-validation-{Guid.NewGuid():N}.db");
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
	.UseSqlite($"Data Source={dbPath}")
	.Options;

try
{
	await using var context = new ApplicationDbContext(options);
	await context.Database.EnsureCreatedAsync();

	var passwordService = new PasswordService();
	var seeder = new DataSeeder(context, passwordService);
	await seeder.SeedAsync();

	await AssertSeedData(context, passwordService);
	await AssertMatching(context);
	await AssertTutoringDistribution(context);
	await AssertSessionsAndFeedbackRules(context);
	await AssertPermissionsAndOperationalRules(context);

	Console.WriteLine("Validação funcional concluída com sucesso.");
	Console.WriteLine("- Seed inicial criado");
	Console.WriteLine("- Credenciais e hashing verificados");
	Console.WriteLine("- Matching automático validado");
	Console.WriteLine("- Distribuição PTM por curso, fase, tutor e mentor validada");
	Console.WriteLine("- Prevenção de matches duplicados validada");
	Console.WriteLine("- Sessões e feedback validados");
	Console.WriteLine("- Permissões, disponibilidade, conflitos e duplicados validados");
}
finally
{
	if (File.Exists(dbPath))
	{
		File.Delete(dbPath);
	}
}

static async Task AssertSeedData(ApplicationDbContext context, PasswordService passwordService)
{
	var users = await context.Users.CountAsync();
	var students = await context.StudentProfiles.CountAsync();
	var mentors = await context.MentorProfiles.CountAsync();
	var matches = await context.MentorshipMatches.CountAsync();
	var sessions = await context.MentoringSessions.CountAsync();
	var feedback = await context.Feedbacks.CountAsync();
	var tutors = await context.TutorProfiles.CountAsync();
	var tutoringAssignments = await context.TutoringAssignments.CountAsync();

	Assert(users >= 5, "Devem existir pelo menos cinco utilizadores seed.");
	Assert(students >= 8, "Devem existir vários alunos seed para validar distribuição realista.");
	Assert(mentors >= 2, "Devem existir mentores seed.");
	Assert(tutors >= 1, "Devem existir tutores docentes seed.");
	Assert(tutoringAssignments >= 1, "Devem existir atribuições de tutoria seed.");
	Assert(matches >= 2, "Devem existir emparelhamentos seed.");
	Assert(sessions >= 3, "Devem existir sessões seed.");
	Assert(feedback >= 2, "Devem existir feedbacks seed.");

	var joao = await context.Users.FirstAsync(user => user.Email == "joao@mentoria.local");
	Assert(passwordService.VerifyPassword("Joao123!", joao.PasswordHash), "A credencial seed do João deve validar.");
}

static async Task AssertTutoringDistribution(ApplicationDbContext context)
{
	var service = new TutoringDistributionService(context);
	await service.DistributeAsync(2);
	var phaseTwoStudents = await context.StudentProfiles
		.CountAsync(profile => profile.AcademicYear == "1" && profile.IsEnrolled && profile.EnrollmentPhase <= 2);
	var phaseTwoActiveAssignments = await context.TutoringAssignments.CountAsync(assignment => assignment.Status == "Active");
	Assert(phaseTwoActiveAssignments == phaseTwoStudents, "A 2.ª fase deve atribuir apenas alunos com matrícula até à 2.ª fase.");
	Assert(!await context.TutoringAssignments.AnyAsync(assignment =>
		assignment.Status == "Active" &&
		assignment.Student.StudentProfile != null &&
		assignment.Student.StudentProfile.EnrollmentPhase == 3), "A 2.ª fase não deve manter alunos da 3.ª fase ativos.");

	await service.DistributeAsync(3);

	var firstYearStudents = await context.StudentProfiles
		.CountAsync(profile => profile.AcademicYear == "1" && profile.IsEnrolled && profile.EnrollmentPhase <= 3);
	var activeAssignments = await context.TutoringAssignments.CountAsync(assignment => assignment.Status == "Active");
	var mentorTutorAssignments = await context.MentorTutorAssignments.CountAsync(assignment => assignment.Status == "Active");
	var allocationWarnings = await context.TutoringAssignments.CountAsync(assignment => assignment.AllocationWarning != "");
	var lowCompatibilityWarnings = await context.TutoringAssignments.CountAsync(assignment => assignment.AllocationWarning.Contains("Compatibilidade baixa"));
	var interestWarnings = await context.TutoringAssignments.CountAsync(assignment => assignment.AllocationWarning.Contains("Sem interesses técnicos em comum"));
	var availabilityWarnings = await context.TutoringAssignments.CountAsync(assignment => assignment.AllocationWarning.Contains("Sem disponibilidade em comum"));
	var rui = await context.Users.FirstAsync(user => user.Email == "rui@mentoria.local");
	var tiago = await context.Users.FirstAsync(user => user.Email == "tiago@mentoria.local");
	var joao = await context.Users.FirstAsync(user => user.Email == "joao@mentoria.local");
	var valentim = await context.Users.FirstAsync(user => user.Email == "valentim@mentoria.local");
	var assignmentWithoutMentor = await context.TutoringAssignments
		.Include(assignment => assignment.Student)
		.ThenInclude(student => student.StudentProfile)
		.FirstOrDefaultAsync(assignment => assignment.Status == "Active" && assignment.MentorId == null);
	var assignmentWithMentor = await context.TutoringAssignments
		.FirstOrDefaultAsync(assignment => assignment.Status == "Active" && assignment.MentorId != null);

	Assert(activeAssignments >= firstYearStudents, "Todos os alunos de 1.º ano ativos devem ficar atribuídos a tutor.");
	Assert(activeAssignments == firstYearStudents, "A distribuição ativa deve ter exatamente uma atribuição por aluno elegível.");
	Assert(!await context.TutoringAssignments
		.Where(assignment => assignment.Status == "Active")
		.GroupBy(assignment => assignment.StudentId)
		.AnyAsync(group => group.Count() > 1), "Não devem existir atribuições ativas duplicadas para o mesmo aluno.");
	Assert(mentorTutorAssignments > 0, "Os mentores devem ser atribuídos a tutores antes dos alunos.");
	Assert(allocationWarnings > 0, "A distribuição deve avisar quando escolhe um mentor por carga apesar de existir mentor com melhor score.");
	Assert(lowCompatibilityWarnings > 0, "A distribuição deve avisar quando a compatibilidade com o mentor atribuído é baixa.");
	Assert(interestWarnings > 0, "A distribuição deve avisar quando aluno e mentor não têm interesses técnicos em comum.");
	Assert(availabilityWarnings > 0, "A distribuição deve avisar quando aluno e mentor não têm disponibilidade em comum.");
	Assert(await context.TutoringAssignments.AnyAsync(assignment =>
		assignment.Status == "Active" &&
		assignment.StudentId == rui.UserId &&
		assignment.MentorId == tiago.UserId), "Rui deve ficar com Tiago porque essa é a melhor combinação global para o seu perfil SQL.");
	Assert(await context.TutoringAssignments.AnyAsync(assignment =>
		assignment.Status == "Active" &&
		assignment.StudentId == joao.UserId &&
		assignment.MentorId == valentim.UserId), "João deve ficar com Valentim porque essa é a melhor combinação global para o seu perfil C#.");
	Assert(assignmentWithoutMentor != null, "Cursos sem mentores devem criar atribuição apenas com tutor.");
	Assert(assignmentWithoutMentor!.TutorId > 0, "A atribuição sem mentor deve manter tutor docente.");
	Assert(assignmentWithoutMentor.NonioTutorListUpdated, "A lista Nónio do tutor deve ficar marcada como atualizada.");
	Assert(assignmentWithoutMentor.TutorEmailSent, "O tutor deve ficar marcado como informado por email.");

	Assert(assignmentWithMentor != null, "Deve existir pelo menos uma atribuição de aluno com mentor.");
	var inheritedTutor = await context.MentorTutorAssignments.FirstOrDefaultAsync(assignment =>
		assignment.Status == "Active" &&
		assignment.MentorId == assignmentWithMentor!.MentorId &&
		assignment.TutorId == assignmentWithMentor.TutorId);
	Assert(inheritedTutor != null, "O aluno com mentor deve herdar o tutor previamente atribuído ao mentor.");

	var mentorLoads = await context.TutoringAssignments
		.Where(assignment => assignment.Status == "Active" && assignment.MentorId != null)
		.GroupBy(assignment => assignment.MentorId!.Value)
		.Select(group => group.Count())
		.ToListAsync();
	Assert(mentorLoads.Count > 1, "Devem existir vários mentores com alunos para validar equilíbrio.");
	Assert(mentorLoads.Max() - mentorLoads.Min() <= 1, "A carga de alunos deve ficar equilibrada entre mentores do curso.");
}

static async Task AssertMatching(ApplicationDbContext context)
{
	var service = new MatchingService(context);
	var bruno = await context.Users.FirstAsync(user => user.Email == "bruno@mentoria.local");

	var recommendations = await service.GetRecommendationsAsync(bruno.UserId);
	Assert(recommendations.Count > 0, "O matching deve gerar recomendações para alunos ativos.");
	Assert(recommendations[0].CompatibilityScore > 0, "A melhor recomendação deve ter score positivo.");

	var firstMatch = await service.CreateBestMatchAsync(bruno.UserId);
	var secondMatch = await service.CreateBestMatchAsync(bruno.UserId);
	Assert(firstMatch != null, "Deve ser possível obter ou criar match para o aluno.");
	Assert(secondMatch != null, "A segunda geração deve devolver o match ativo existente.");
	Assert(firstMatch!.MatchId == secondMatch!.MatchId, "A geração automática não deve duplicar matches ativos do mesmo aluno.");
}

static async Task AssertSessionsAndFeedbackRules(ApplicationDbContext context)
{
	var activeMatch = await context.MentorshipMatches.FirstAsync(match => match.Status == "Active");
	var session = new MentoringSession
	{
		MatchId = activeMatch.MatchId,
		ScheduledAt = DateTime.UtcNow.AddDays(1),
		DurationMinutes = 45,
		Topic = "Validação funcional",
		Notes = "Sessão criada pelo validador.",
		Status = "Completed",
		CompletedAt = DateTime.UtcNow
	};

	context.MentoringSessions.Add(session);
	await context.SaveChangesAsync();

	var feedback = new Feedback
	{
		SessionId = session.SessionId,
		AuthorUserId = activeMatch.StudentId,
		TargetUserId = activeMatch.MentorId,
		Rating = 5,
		Comment = "Fluxo validado."
	};

	context.Feedbacks.Add(feedback);
	await context.SaveChangesAsync();

	Assert(await context.Feedbacks.AnyAsync(item => item.Comment == "Fluxo validado."), "O feedback de validação deve ser persistido.");
}

static async Task AssertPermissionsAndOperationalRules(ApplicationDbContext context)
{
	var joao = await context.Users.FirstAsync(user => user.Email == "joao@mentoria.local");
	var valentim = await context.Users.FirstAsync(user => user.Email == "valentim@mentoria.local");
	var diogo = await context.Users.FirstAsync(user => user.Email == "diogo@mentoria.local");
	var joaoMatch = await context.MentorshipMatches.FirstAsync(match =>
		match.Status == "Active" &&
		match.StudentId == joao.UserId &&
		match.MentorId == valentim.UserId);
	var diogoMatch = await context.MentorshipMatches.FirstAsync(match =>
		match.Status == "Active" &&
		match.StudentId == diogo.UserId &&
		match.MentorId == valentim.UserId);

	var studentsController = WithUser(new StudentsController(context), joao.UserId, "Student");
	var studentResult = await studentsController.GetStudents();
	var studentOk = AssertOk(studentResult);
	Assert(AsEnumerable(studentOk.Value).Count() == 1, "Aluno deve ver apenas o seu perfil académico.");

	var mentorsController = WithUser(new MentorsController(context), joao.UserId, "Student");
	var mentorResult = await mentorsController.GetMentors();
	var mentorOk = AssertOk(mentorResult);
	Assert(AsEnumerable(mentorOk.Value).Count() == 1, "Aluno deve ver apenas o seu mentor associado.");

	var tutoringController = WithUser(new TutoringController(new TutoringDistributionService(context)), joao.UserId, "Student");
	var overviewResult = await tutoringController.GetOverview();
	var overview = AssertOk(overviewResult).Value!;
	var assignments = AsEnumerable(GetProperty(overview, "Assignments")).ToList();
	Assert(assignments.Count == 1, "Aluno deve ver apenas a sua atribuição PTM.");
	Assert((string)GetProperty(assignments[0], "Tutor") == "Oculto para aluno", "Aluno não deve ver o tutor atribuído ao mentor.");

	var sessionsController = WithUser(new SessionsController(context), valentim.UserId, "Mentor");
	var invalidAvailability = await sessionsController.CreateSession(new CreateSessionRequest
	{
		MatchId = joaoMatch.MatchId,
		ScheduledAt = NextLocalSlot(DayOfWeek.Friday, 10),
		DurationMinutes = 45,
		Topic = "Disponibilidade inválida",
		Notes = "Validação"
	});
	Assert(invalidAvailability is BadRequestObjectResult, "Sessão fora da disponibilidade deve ser rejeitada.");

	var validSlot = NextLocalSlot(DayOfWeek.Thursday, 10);
	var createResult = await sessionsController.CreateSession(new CreateSessionRequest
	{
		MatchId = joaoMatch.MatchId,
		ScheduledAt = validSlot,
		DurationMinutes = 45,
		Topic = "Sessão validada",
		Notes = "Validação"
	});
	Assert(createResult is CreatedAtActionResult, "Sessão dentro da disponibilidade deve ser criada.");
	var createdSession = (MentoringSession)((CreatedAtActionResult)createResult).Value!;

	var conflictResult = await sessionsController.CreateSession(new CreateSessionRequest
	{
		MatchId = diogoMatch.MatchId,
		ScheduledAt = validSlot.AddMinutes(15),
		DurationMinutes = 45,
		Topic = "Sessão sobreposta",
		Notes = "Validação"
	});
	Assert(conflictResult is ConflictObjectResult, "Sessão sobreposta deve ser bloqueada.");

	var rescheduleResult = await sessionsController.Reschedule(createdSession.SessionId, new RescheduleSessionRequest
	{
		ScheduledAt = validSlot.AddHours(2),
		DurationMinutes = 45,
		Topic = "Sessão remarcada",
		Notes = "Validação remarcada"
	});
	Assert(rescheduleResult is OkObjectResult, "Sessão agendada deve poder ser remarcada pelo mentor.");

	var cancelResult = await sessionsController.UpdateStatus(createdSession.SessionId, new UpdateSessionStatusRequest
	{
		Status = "Cancelled",
		Notes = "Cancelada pelo validador."
	});
	Assert(cancelResult is OkObjectResult, "Sessão agendada deve poder ser cancelada pelo mentor.");

	var feedbackController = WithUser(new FeedbackController(context), valentim.UserId, "Mentor");
	var existingSession = await context.MentoringSessions
		.Include(session => session.Match)
		.FirstAsync(session =>
			session.Status == "Completed" &&
			session.Match.StudentId == joao.UserId &&
			session.Match.MentorId == valentim.UserId);
	var duplicateFeedback = await feedbackController.CreateFeedback(new CreateFeedbackRequest
	{
		SessionId = existingSession.SessionId,
		AuthorUserId = valentim.UserId,
		TargetUserId = joao.UserId,
		Rating = 4,
		Comment = "Duplicado"
	});
	Assert(duplicateFeedback is ConflictObjectResult, "Feedback duplicado do mesmo autor para a mesma sessão deve ser rejeitado.");
}

static T WithUser<T>(T controller, int userId, params string[] roles) where T : ControllerBase
{
	var claims = new List<Claim>
	{
		new("sub", userId.ToString())
	};
	claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
	controller.ControllerContext = new ControllerContext
	{
		HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Validation"))
		}
	};
	return controller;
}

static OkObjectResult AssertOk(ActionResult result)
{
	Assert(result is OkObjectResult, "A ação deve devolver OkObjectResult.");
	return (OkObjectResult)result;
}

static IEnumerable<object> AsEnumerable(object? value)
{
	return ((System.Collections.IEnumerable)value!).Cast<object>();
}

static object GetProperty(object value, string name)
{
	return value.GetType().GetProperty(name)!.GetValue(value)!;
}

static DateTime NextLocalSlot(DayOfWeek day, int hour)
{
	var date = DateTime.Today.AddDays(14);
	while (date.DayOfWeek != day)
	{
		date = date.AddDays(1);
	}

	return date.AddHours(hour);
}

static void Assert(bool condition, string message)
{
	if (!condition)
	{
		throw new InvalidOperationException(message);
	}
}
