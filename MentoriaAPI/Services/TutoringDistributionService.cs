using MentoriaAPI.Data;
using MentoriaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MentoriaAPI.Services
{
	public class TutoringDistributionService
	{
		private readonly ApplicationDbContext _context;

		public TutoringDistributionService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<TutoringAssignment>> DistributeAsync(int phase)
		{
			var students = await _context.Users
				.Include(u => u.StudentProfile)
				.Where(u =>
					u.IsActive &&
					u.StudentProfile != null &&
					u.StudentProfile.AcademicYear == "1" &&
					u.StudentProfile.IsEnrolled &&
					u.StudentProfile.EnrollmentPhase <= phase)
				.OrderBy(u => u.StudentProfile!.Course)
				.ThenBy(u => u.Name)
				.ToListAsync();

			var enrolledStudentIds = await _context.Users
				.Include(u => u.StudentProfile)
				.Where(u =>
					u.IsActive &&
					u.StudentProfile != null &&
					u.StudentProfile.AcademicYear == "1" &&
					u.StudentProfile.IsEnrolled)
				.Select(u => u.UserId)
				.ToListAsync();
			var activeStudentIds = enrolledStudentIds.ToHashSet();
			var obsoleteAssignments = await _context.TutoringAssignments
				.Where(a => a.Status == "Active" && !activeStudentIds.Contains(a.StudentId))
				.ToListAsync();
			foreach (var assignment in obsoleteAssignments)
			{
				assignment.Status = "Inactive";
				assignment.Summary = "Aluno removido da fase atual por abandono ou atualização de matrícula.";
			}

			var existingAssignments = (await _context.TutoringAssignments
				.OrderByDescending(a => a.Status == "Active")
				.ThenByDescending(a => a.AssignedAt)
				.ToListAsync())
				.GroupBy(a => a.StudentId)
				.ToDictionary(group => group.Key, group => group.First());

			var createdOrUpdated = new List<TutoringAssignment>();
			foreach (var courseGroup in students.GroupBy(s => s.StudentProfile!.Course))
			{
				var course = courseGroup.Key;
				var mentors = await GetMentorsForCourseAsync(course);
				var tutors = await GetTutorsForCourseAsync(course);
				if (tutors.Count == 0)
				{
					continue;
				}

				var reassignedStudentIds = courseGroup.Select(student => student.UserId).ToList();
				var mentorTutorAssignments = await AssignMentorsToTutorsAsync(course, mentors, tutors);
				var studentLoadByMentor = await BuildMentorStudentLoadAsync(mentors, reassignedStudentIds);
				var directStudentLoadByTutor = await BuildDirectTutorStudentLoadAsync(tutors, reassignedStudentIds);
				var optimizedMentors = BuildOptimizedMentorAssignments(courseGroup.ToList(), mentors, studentLoadByMentor);

				foreach (var student in courseGroup)
				{
					var mentorScores = mentors.ToDictionary(m => m.UserId, m => BuildMentorScore(student, m));
					var mentor = optimizedMentors.TryGetValue(student.UserId, out var optimizedMentor) ? optimizedMentor : null;
					var tutor = mentor == null
						? tutors.MinBy(t => directStudentLoadByTutor[t.UserId])!
						: mentorTutorAssignments[mentor.UserId].Tutor;
					var allocationWarning = mentor == null
						? string.Empty
						: BuildAllocationWarning(student, mentor, mentors, mentorScores);

					if (mentor != null)
					{
						studentLoadByMentor[mentor.UserId]++;
					}
					else
					{
						directStudentLoadByTutor[tutor.UserId]++;
					}

					if (!existingAssignments.TryGetValue(student.UserId, out var assignment))
					{
						assignment = new TutoringAssignment
						{
							StudentId = student.UserId
						};
						_context.TutoringAssignments.Add(assignment);
					}

					assignment.Status = "Active";
					assignment.MentorId = mentor?.UserId;
					assignment.TutorId = tutor.UserId;
					assignment.Course = course;
					assignment.EnrollmentPhase = phase;
					assignment.AssignedAt = DateTime.UtcNow;
					assignment.MentorEmailSent = mentor != null;
					assignment.TutorEmailSent = true;
					assignment.NonioTutorListUpdated = true;
					assignment.Summary = mentor == null
						? "Curso sem mentores disponíveis: aluno atribuído apenas ao tutor docente."
						: "Aluno atribuído ao mentor e herdou o tutor previamente atribuído ao mentor.";
					assignment.AllocationWarning = allocationWarning;

					if (mentor != null)
					{
						await EnsureMentorshipMatchAsync(student, mentor, mentorScores[mentor.UserId]);
					}

					createdOrUpdated.Add(assignment);
				}
			}

			await DeactivateMatchesOutsideActiveTutoringAsync();
			await RemoveInactiveDuplicateTutoringAssignmentsAsync();
			await _context.SaveChangesAsync();
			return createdOrUpdated;
		}

		public async Task<object> GetOverviewAsync()
		{
			return await GetOverviewAsync(null, true);
		}

		public async Task<object> GetOverviewAsync(int? userId, bool isAdmin, bool hideTutorDetails = false)
		{
			var assignments = await _context.TutoringAssignments
				.Include(a => a.Student)
				.Include(a => a.Mentor)
				.Include(a => a.Tutor)
				.Where(a =>
					a.Status == "Active" &&
					(isAdmin ||
						a.StudentId == userId ||
						a.MentorId == userId ||
						a.TutorId == userId))
				.OrderBy(a => a.Course)
				.ThenBy(a => a.Student.Name)
				.Select(a => new
				{
					a.AssignmentId,
					a.StudentId,
					Student = a.Student.Name,
					MentorId = a.MentorId,
					Mentor = a.Mentor == null ? "Sem mentor no curso" : a.Mentor.Name,
					TutorId = hideTutorDetails ? 0 : a.TutorId,
					Tutor = hideTutorDetails ? "Oculto para aluno" : a.Tutor.Name,
					a.Course,
					a.EnrollmentPhase,
					a.Status,
					a.Summary,
					a.AllocationWarning,
					a.MentorEmailSent,
					a.TutorEmailSent,
					a.NonioTutorListUpdated,
					a.AssignedAt
				})
				.ToListAsync();

			return new
			{
				TotalTutors = hideTutorDetails
					? 0
					: isAdmin
					? await _context.TutorProfiles.CountAsync()
					: assignments.Select(a => a.TutorId).Distinct().Count(),
				ActiveAssignments = assignments.Count(a => a.Status == "Active"),
				WithoutMentor = assignments.Count(a => a.Status == "Active" && a.MentorId == null),
				NonioUpdated = assignments.Count(a => a.Status == "Active" && a.NonioTutorListUpdated),
				Assignments = assignments
			};
		}

		private async Task<List<User>> GetMentorsForCourseAsync(string course)
		{
			return await _context.Users
				.Include(u => u.MentorProfile)
				.Include(u => u.MentorTutoringAssignments)
				.Where(u =>
					u.IsActive &&
					u.MentorProfile != null &&
					u.MentorProfile.Course == course &&
					(u.MentorProfile.AcademicYear == 2 || u.MentorProfile.AcademicYear == 3))
				.ToListAsync();
		}

		private async Task<List<User>> GetTutorsForCourseAsync(string course)
		{
			return await _context.Users
				.Include(u => u.TutorProfile)
				.Include(u => u.TutorAssignments)
				.Where(u => u.IsActive && u.TutorProfile != null)
				.ToListAsync()
				.ContinueWith(task => task.Result
					.Where(t => t.TutorProfile!.Courses
						.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
						.Contains(course, StringComparer.OrdinalIgnoreCase))
					.ToList());
		}

		private async Task<Dictionary<int, MentorTutorAssignment>> AssignMentorsToTutorsAsync(
			string course,
			List<User> mentors,
			List<User> tutors)
		{
			var mentorIds = mentors.Select(m => m.UserId).ToList();
			var existingAssignments = await _context.MentorTutorAssignments
				.Include(a => a.Tutor)
				.Where(a => a.Status == "Active" && mentorIds.Contains(a.MentorId))
				.ToDictionaryAsync(a => a.MentorId);
			var tutorLoad = await BuildMentorLoadByTutorAsync(tutors, mentorIds);
			var result = new Dictionary<int, MentorTutorAssignment>();

			foreach (var mentor in mentors)
			{
				var tutor = tutors.MinBy(t => tutorLoad[t.UserId])!;
				if (!existingAssignments.TryGetValue(mentor.UserId, out var assignment))
				{
					assignment = new MentorTutorAssignment
					{
						MentorId = mentor.UserId,
						Status = "Active"
					};
					_context.MentorTutorAssignments.Add(assignment);
				}

				assignment.TutorId = tutor.UserId;
				assignment.Tutor = tutor;
				assignment.Course = course;
				assignment.AssignedAt = DateTime.UtcNow;
				assignment.TutorEmailSent = true;
				assignment.Summary = "Mentor atribuído ao tutor antes da distribuição de alunos.";
				tutorLoad[tutor.UserId]++;
				result[mentor.UserId] = assignment;
			}

			return result;
		}

		private async Task<Dictionary<int, int>> BuildMentorLoadByTutorAsync(List<User> tutors, List<int> reassignedMentorIds)
		{
			var ids = tutors.Select(t => t.UserId).ToList();
			var loads = await _context.MentorTutorAssignments
				.Where(a => a.Status == "Active" && ids.Contains(a.TutorId) && !reassignedMentorIds.Contains(a.MentorId))
				.GroupBy(a => a.TutorId)
				.Select(g => new { UserId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.UserId, x => x.Count);

			foreach (var tutor in tutors)
			{
				loads.TryAdd(tutor.UserId, 0);
			}

			return loads;
		}

		private static Dictionary<int, User> BuildOptimizedMentorAssignments(
			List<User> students,
			List<User> mentors,
			Dictionary<int, int> existingLoad)
		{
			if (mentors.Count == 0)
			{
				return new Dictionary<int, User>();
			}

			var orderedStudents = students
				.OrderByDescending(student => mentors.Max(mentor => BuildMentorScore(student, mentor).Score) - mentors.Min(mentor => BuildMentorScore(student, mentor).Score))
				.ThenBy(student => student.Name)
				.ToList();
			var totalAssignments = orderedStudents.Count;
			var currentLoads = mentors.ToDictionary(mentor => mentor.UserId, mentor => existingLoad.GetValueOrDefault(mentor.UserId));
			var maxLoads = mentors.ToDictionary(mentor => mentor.UserId, mentor => mentor.MentorProfile!.MaxStudents);
			var targetLoads = BuildBalancedTargetLoads(mentors, currentLoads, totalAssignments);
			var bestScore = int.MinValue;
			var bestPenalty = int.MaxValue;
			var bestAssignment = new Dictionary<int, User>();
			var currentAssignment = new Dictionary<int, User>();

			Search(0, 0);
			return bestAssignment;

			void Search(int studentIndex, int score)
			{
				if (studentIndex == orderedStudents.Count)
				{
					var penalty = mentors.Sum(mentor => Math.Abs(currentLoads[mentor.UserId] - targetLoads[mentor.UserId]));
					if (score > bestScore || (score == bestScore && penalty < bestPenalty))
					{
						bestScore = score;
						bestPenalty = penalty;
						bestAssignment = new Dictionary<int, User>(currentAssignment);
					}
					return;
				}

				var student = orderedStudents[studentIndex];
				foreach (var mentor in mentors
					.OrderBy(mentor => currentLoads[mentor.UserId] >= maxLoads[mentor.UserId])
					.ThenByDescending(mentor => BuildMentorScore(student, mentor).Score)
					.ThenBy(mentor => mentor.Name))
				{
					var mentorId = mentor.UserId;
					var overCapacity = currentLoads[mentorId] >= maxLoads[mentorId];
					if (overCapacity && mentors.Any(other => currentLoads[other.UserId] < maxLoads[other.UserId]))
					{
						continue;
					}

					currentLoads[mentorId]++;
					currentAssignment[student.UserId] = mentor;
					Search(studentIndex + 1, score + BuildMentorScore(student, mentor).Score);
					currentAssignment.Remove(student.UserId);
					currentLoads[mentorId]--;
				}
			}
		}

		private static Dictionary<int, int> BuildBalancedTargetLoads(List<User> mentors, Dictionary<int, int> currentLoads, int newAssignments)
		{
			var targets = mentors.ToDictionary(mentor => mentor.UserId, mentor => currentLoads[mentor.UserId]);
			for (var i = 0; i < newAssignments; i++)
			{
				var nextMentor = mentors
					.OrderBy(mentor => targets[mentor.UserId])
					.ThenBy(mentor => mentor.Name)
					.First();
				targets[nextMentor.UserId]++;
			}

			return targets;
		}

		private async Task<Dictionary<int, int>> BuildMentorStudentLoadAsync(List<User> mentors, List<int> reassignedStudentIds)
		{
			var ids = mentors.Select(m => m.UserId).ToList();
			var loads = await _context.TutoringAssignments
				.Where(a =>
					a.Status == "Active" &&
					a.MentorId != null &&
					ids.Contains(a.MentorId.Value) &&
					!reassignedStudentIds.Contains(a.StudentId))
				.GroupBy(a => a.MentorId!.Value)
				.Select(g => new { UserId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.UserId, x => x.Count);

			foreach (var mentor in mentors)
			{
				loads.TryAdd(mentor.UserId, 0);
			}

			return loads;
		}

		private async Task<Dictionary<int, int>> BuildDirectTutorStudentLoadAsync(List<User> tutors, List<int> reassignedStudentIds)
		{
			var ids = tutors.Select(t => t.UserId).ToList();
			var loads = await _context.TutoringAssignments
				.Where(a =>
					a.Status == "Active" &&
					a.MentorId == null &&
					ids.Contains(a.TutorId) &&
					!reassignedStudentIds.Contains(a.StudentId))
				.GroupBy(a => a.TutorId)
				.Select(g => new { UserId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.UserId, x => x.Count);

			foreach (var tutor in tutors)
			{
				loads.TryAdd(tutor.UserId, 0);
			}

			return loads;
		}

		private static string BuildAllocationWarning(
			User student,
			User selectedMentor,
			List<User> mentors,
			Dictionary<int, MentorScore> mentorScores)
		{
			var warnings = new List<string>();
			var selectedScore = mentorScores[selectedMentor.UserId];
			var bestMentor = mentors
				.OrderByDescending(m => mentorScores[m.UserId].Score)
				.ThenBy(m => m.Name)
				.FirstOrDefault();

			if (bestMentor != null && bestMentor.UserId != selectedMentor.UserId)
			{
				var bestScore = mentorScores[bestMentor.UserId];
				if (bestScore.Score > selectedScore.Score)
				{
					warnings.Add($"{bestMentor.Name} teria melhor compatibilidade para {student.Name} (score {bestScore.Score} vs {selectedScore.Score}), mas {selectedMentor.Name} foi escolhido para manter a distribuição uniforme por carga.");
				}
			}

			if (selectedScore.Score < 60)
			{
				warnings.Add($"Compatibilidade baixa com {selectedMentor.Name} (score {selectedScore.Score}). Recomenda-se acompanhamento inicial mais próximo pelo tutor.");
			}

			if (selectedScore.InterestOverlap == 0)
			{
				warnings.Add($"Sem interesses técnicos em comum entre {student.Name} e {selectedMentor.Name}.");
			}

			if (selectedScore.AvailabilityOverlap == 0)
			{
				warnings.Add($"Sem disponibilidade em comum registada entre {student.Name} e {selectedMentor.Name}.");
			}

			return warnings.Count == 0 ? string.Empty : $"Aviso: {string.Join(" ", warnings)}";
		}

		private static MentorScore BuildMentorScore(User student, User mentor)
		{
			var studentProfile = student.StudentProfile!;
			var mentorProfile = mentor.MentorProfile!;
			var score = 0;

			if (string.Equals(studentProfile.Course, mentorProfile.Course, StringComparison.OrdinalIgnoreCase))
			{
				score += 40;
			}

			var interestOverlap = CountOverlap(student.AreasOfInterest, mentorProfile.AreasOfExpertise);
			var availabilityOverlap = CountOverlap(student.Availability, mentorProfile.Availability);
			score += Math.Min(interestOverlap * 15, 30);
			score += Math.Min(availabilityOverlap * 8, 16);

			if (!string.IsNullOrWhiteSpace(studentProfile.Goals) &&
				studentProfile.Goals.Contains(mentorProfile.Specialization, StringComparison.OrdinalIgnoreCase))
			{
				score += 14;
			}

			if (studentProfile.AverageGrade < 13 && mentorProfile.YearsOfExperience >= 2)
			{
				score += 5;
			}

			return new MentorScore(Math.Clamp(score, 0, 100), interestOverlap, availabilityOverlap);
		}

		private async Task EnsureMentorshipMatchAsync(User student, User mentor, MentorScore score)
		{
			var activeMatches = await _context.MentorshipMatches
				.Where(match => match.StudentId == student.UserId && match.Status == "Active")
				.ToListAsync();
			var match = activeMatches.FirstOrDefault(item => item.MentorId == mentor.UserId);

			foreach (var oldMatch in activeMatches.Where(item => item.MentorId != mentor.UserId))
			{
				oldMatch.Status = "Inactive";
				oldMatch.Summary = "Substituído pela distribuição oficial PTM.";
			}

			if (match == null)
			{
				match = new MentorshipMatch
				{
					StudentId = student.UserId,
					MentorId = mentor.UserId,
					Status = "Active",
					CreatedAt = DateTime.UtcNow
				};
				_context.MentorshipMatches.Add(match);
			}

			match.CompatibilityScore = score.Score;
			match.Summary = $"Emparelhamento sincronizado pela distribuição PTM. Score {score.Score}.";
		}

		private async Task DeactivateMatchesOutsideActiveTutoringAsync()
		{
			var activeTutoringPairs = await _context.TutoringAssignments
				.Where(assignment => assignment.Status == "Active" && assignment.MentorId != null)
				.Select(assignment => new { assignment.StudentId, MentorId = assignment.MentorId!.Value })
				.ToListAsync();
			var activePairKeys = activeTutoringPairs
				.Select(pair => $"{pair.StudentId}:{pair.MentorId}")
				.ToHashSet();
			var activeMatches = await _context.MentorshipMatches
				.Where(match => match.Status == "Active")
				.ToListAsync();

			foreach (var match in activeMatches.Where(match => !activePairKeys.Contains($"{match.StudentId}:{match.MentorId}")))
			{
				match.Status = "Inactive";
				match.Summary = "Inativo por não corresponder à distribuição PTM atual.";
			}
		}

		private async Task RemoveInactiveDuplicateTutoringAssignmentsAsync()
		{
			var activeStudentIds = await _context.TutoringAssignments
				.Where(assignment => assignment.Status == "Active")
				.Select(assignment => assignment.StudentId)
				.ToListAsync();
			var inactiveDuplicates = await _context.TutoringAssignments
				.Where(assignment => assignment.Status != "Active" && activeStudentIds.Contains(assignment.StudentId))
				.ToListAsync();

			_context.TutoringAssignments.RemoveRange(inactiveDuplicates);
		}

		private static int CountOverlap(string left, string right)
		{
			var leftTokens = left.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(token => token.ToLowerInvariant())
				.ToHashSet();
			var rightTokens = right.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(token => token.ToLowerInvariant())
				.ToHashSet();

			return leftTokens.Intersect(rightTokens).Count();
		}

		private sealed record MentorScore(int Score, int InterestOverlap, int AvailabilityOverlap);
	}
}
