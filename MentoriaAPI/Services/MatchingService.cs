using MentoriaAPI.Data;
using MentoriaAPI.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MentoriaAPI.Services
{
	public class MatchingService
	{
		private readonly ApplicationDbContext _context;

		public MatchingService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<MatchRecommendationDto>> GetRecommendationsAsync(int studentId)
		{
			var student = await _context.Users
				.Include(u => u.StudentProfile)
				.Include(u => u.UserRoles)
				.ThenInclude(ur => ur.Role)
				.FirstOrDefaultAsync(u => u.UserId == studentId);

			if (student?.StudentProfile == null)
			{
				return new List<MatchRecommendationDto>();
			}

			var mentors = await _context.Users
				.Include(u => u.MentorProfile)
				.Include(u => u.MentorMatches)
				.Where(u => u.MentorProfile != null && u.IsActive)
				.ToListAsync();

			return mentors
				.Select(mentor => BuildRecommendation(student, mentor))
				.Where(rec => rec.CompatibilityScore > 0)
				.OrderByDescending(rec => rec.CompatibilityScore)
				.ToList();
		}

		public async Task<Models.MentorshipMatch?> CreateBestMatchAsync(int studentId)
		{
			var existingActiveMatch = await _context.MentorshipMatches
				.FirstOrDefaultAsync(match => match.StudentId == studentId && match.Status == "Active");
			if (existingActiveMatch != null)
			{
				await SyncTutoringAssignmentAsync(existingActiveMatch);
				return existingActiveMatch;
			}

			var recommendations = await GetRecommendationsAsync(studentId);
			var best = recommendations.FirstOrDefault();
			if (best == null)
			{
				return null;
			}

			var match = new Models.MentorshipMatch
			{
				StudentId = studentId,
				MentorId = best.MentorId,
				CompatibilityScore = best.CompatibilityScore,
				Status = "Active",
				Summary = best.Summary
			};

			_context.MentorshipMatches.Add(match);
			await _context.SaveChangesAsync();
			await SyncTutoringAssignmentAsync(match);
			return match;
		}

		public async Task SyncTutoringAssignmentAsync(Models.MentorshipMatch match)
		{
			var student = await _context.Users
				.Include(u => u.StudentProfile)
				.FirstOrDefaultAsync(u => u.UserId == match.StudentId);
			var mentor = await _context.Users
				.Include(u => u.MentorProfile)
				.FirstOrDefaultAsync(u => u.UserId == match.MentorId);

			if (student?.StudentProfile == null || mentor?.MentorProfile == null)
			{
				return;
			}

			var studentCourse = student.StudentProfile.Course;
			var existingMentorTutor = await _context.MentorTutorAssignments
				.Include(a => a.Tutor)
				.ThenInclude(t => t.TutorProfile)
				.FirstOrDefaultAsync(a => a.Status == "Active" && a.MentorId == mentor.UserId);
			var tutor = existingMentorTutor?.Tutor != null && TutorSupportsCourse(existingMentorTutor.Tutor, studentCourse)
				? existingMentorTutor.Tutor
				: await FindTutorForCourseAsync(studentCourse);
			if (tutor == null)
			{
				return;
			}

			if (existingMentorTutor == null)
			{
				_context.MentorTutorAssignments.Add(new Models.MentorTutorAssignment
				{
					MentorId = mentor.UserId,
					TutorId = tutor.UserId,
					Course = studentCourse,
					Status = "Active",
					Summary = "Mentor atribuído ao tutor pelo emparelhamento automático.",
					TutorEmailSent = true,
					AssignedAt = DateTime.UtcNow
				});
			}
			else
			{
				existingMentorTutor.TutorId = tutor.UserId;
				existingMentorTutor.Course = studentCourse;
				existingMentorTutor.Status = "Active";
				existingMentorTutor.Summary = "Mentor atribuído ao tutor pelo emparelhamento automático.";
				existingMentorTutor.TutorEmailSent = true;
				existingMentorTutor.AssignedAt = DateTime.UtcNow;
			}

			var assignment = await _context.TutoringAssignments
				.OrderByDescending(a => a.Status == "Active")
				.ThenByDescending(a => a.AssignedAt)
				.FirstOrDefaultAsync(a => a.StudentId == student.UserId);

			if (assignment == null)
			{
				assignment = new Models.TutoringAssignment
				{
					StudentId = student.UserId
				};
				_context.TutoringAssignments.Add(assignment);
			}

			assignment.MentorId = mentor.UserId;
			assignment.TutorId = tutor.UserId;
			assignment.Course = studentCourse;
			assignment.EnrollmentPhase = student.StudentProfile.EnrollmentPhase;
			assignment.Status = "Active";
			assignment.Summary = "Aluno atribuído ao mentor e tutor pelo emparelhamento automático.";
			assignment.AllocationWarning = string.Empty;
			assignment.MentorEmailSent = true;
			assignment.TutorEmailSent = true;
			assignment.NonioTutorListUpdated = true;
			assignment.AssignedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
		}

		private async Task<Models.User?> FindTutorForCourseAsync(string course)
		{
			var tutors = await _context.Users
				.Include(u => u.TutorProfile)
				.Include(u => u.TutorAssignments)
				.Where(u => u.IsActive && u.TutorProfile != null)
				.ToListAsync();

			return tutors
				.Where(t => TutorSupportsCourse(t, course))
				.OrderBy(t => t.TutorAssignments.Count(a => a.Status == "Active"))
				.ThenBy(t => t.Name)
				.FirstOrDefault();
		}

		private static bool TutorSupportsCourse(Models.User tutor, string course)
		{
			return tutor.TutorProfile != null &&
				tutor.TutorProfile.Courses
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Contains(course, StringComparer.OrdinalIgnoreCase);
		}

		private MatchRecommendationDto BuildRecommendation(Models.User student, Models.User mentor)
		{
			var mentorProfile = mentor.MentorProfile!;
			var studentProfile = student.StudentProfile!;
			decimal score = 0;
			var notes = new List<string>();

			if (string.Equals(studentProfile.Course, mentorProfile.Course, StringComparison.OrdinalIgnoreCase))
			{
				score += 35;
				notes.Add("mesmo curso");
			}

			var interestOverlap = CountOverlap(student.AreasOfInterest, mentorProfile.AreasOfExpertise);
			if (interestOverlap > 0)
			{
				score += Math.Min(interestOverlap * 10, 25);
				notes.Add("interesses alinhados");
			}

			var availabilityOverlap = CountOverlap(student.Availability, mentorProfile.Availability);
			if (availabilityOverlap > 0)
			{
				score += Math.Min(availabilityOverlap * 8, 16);
				notes.Add("disponibilidade compatível");
			}

			if (!string.IsNullOrWhiteSpace(studentProfile.Goals) &&
				studentProfile.Goals.Contains(mentorProfile.Specialization, StringComparison.OrdinalIgnoreCase))
			{
				score += 14;
				notes.Add("especialização relevante");
			}

			var currentLoad = mentor.MentorMatches.Count(m => m.Status == "Active");
			if (currentLoad < mentorProfile.MaxStudents)
			{
				score += 10;
				notes.Add("mentor com capacidade");
			}
			else
			{
				return new MatchRecommendationDto
				{
					MentorId = mentor.UserId,
					MentorName = mentor.Name,
					Course = mentorProfile.Course,
					Specialization = mentorProfile.Specialization,
					CompatibilityScore = 0,
					Summary = "Mentor sem capacidade disponível."
				};
			}

			if (studentProfile.AverageGrade < 13 && mentorProfile.YearsOfExperience >= 2)
			{
				score += 5;
				notes.Add("perfil adequado para acompanhamento");
			}

			score = Math.Clamp(score, 0, 100);

			return new MatchRecommendationDto
			{
				MentorId = mentor.UserId,
				MentorName = mentor.Name,
				Course = mentorProfile.Course,
				Specialization = mentorProfile.Specialization,
				CompatibilityScore = score,
				Summary = notes.Count == 0 ? "Compatibilidade base." : string.Join(", ", notes)
			};
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
	}
}
