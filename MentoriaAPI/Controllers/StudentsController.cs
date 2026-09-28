using MentoriaAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MentoriaAPI.Controllers
{
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class StudentsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public StudentsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetStudents()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var isMentor = User.IsInRole("Mentor");
			var assignedStudentIds = isMentor
				? (await _context.MentorshipMatches
						.Where(match => match.Status == "Active" && match.MentorId == userId)
						.Select(match => match.StudentId)
						.ToListAsync())
					.Concat(await _context.TutoringAssignments
						.Where(assignment => assignment.Status == "Active" && assignment.MentorId == userId)
						.Select(assignment => assignment.StudentId)
						.ToListAsync())
					.Distinct()
					.ToList()
				: new List<int>();

			var students = await _context.Users
				.Include(u => u.StudentProfile)
				.Where(u =>
					u.StudentProfile != null &&
					(isAdmin || u.UserId == userId || assignedStudentIds.Contains(u.UserId)))
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					u.AreasOfInterest,
					u.Availability,
					Course = u.StudentProfile!.Course,
					AcademicYear = u.StudentProfile.AcademicYear,
					u.StudentProfile.Goals,
					u.StudentProfile.AverageGrade,
					u.StudentProfile.EnrollmentPhase,
					u.StudentProfile.IsEnrolled
				})
				.ToListAsync();

			return Ok(students);
		}

		[HttpGet("{id:int}")]
		public async Task<ActionResult> GetStudent(int id)
		{
			var userId = CurrentUserId();
			var isAllowed = User.IsInRole("Admin") ||
				id == userId ||
				await _context.MentorshipMatches.AnyAsync(match =>
					match.Status == "Active" &&
					match.StudentId == id &&
					match.MentorId == userId) ||
				await _context.TutoringAssignments.AnyAsync(assignment =>
					assignment.Status == "Active" &&
					assignment.StudentId == id &&
					assignment.MentorId == userId);
			if (!isAllowed)
			{
				return Forbid();
			}

			var student = await _context.Users
				.Include(u => u.StudentProfile)
				.Where(u => u.UserId == id && u.StudentProfile != null)
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					u.Bio,
					u.AreasOfInterest,
					u.Availability,
					u.StudentProfile
				})
				.FirstOrDefaultAsync();

			return student == null ? NotFound() : Ok(student);
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
