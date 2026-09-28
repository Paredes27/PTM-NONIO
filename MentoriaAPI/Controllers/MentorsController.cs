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
	public class MentorsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public MentorsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetMentors()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var studentMentorIds = User.IsInRole("Student")
				? (await _context.MentorshipMatches
						.Where(match => match.Status == "Active" && match.StudentId == userId)
						.Select(match => match.MentorId)
						.ToListAsync())
					.Concat(await _context.TutoringAssignments
						.Where(assignment => assignment.Status == "Active" && assignment.StudentId == userId && assignment.MentorId != null)
						.Select(assignment => assignment.MentorId!.Value)
						.ToListAsync())
					.Distinct()
					.ToList()
				: new List<int>();

			var mentors = await _context.Users
				.Include(u => u.MentorProfile)
				.Include(u => u.MentorMatches)
				.Where(u =>
					u.MentorProfile != null &&
					(isAdmin || u.UserId == userId || studentMentorIds.Contains(u.UserId)))
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					u.AreasOfInterest,
					u.Availability,
					Course = u.MentorProfile!.Course,
					u.MentorProfile.Specialization,
					u.MentorProfile.AreasOfExpertise,
					u.MentorProfile.MaxStudents,
					u.MentorProfile.YearsOfExperience,
					u.MentorProfile.AcademicYear,
					CurrentStudents = u.MentorMatches.Count(m => m.Status == "Active")
				})
				.ToListAsync();

			return Ok(mentors);
		}

		[HttpGet("{id:int}")]
		public async Task<ActionResult> GetMentor(int id)
		{
			var userId = CurrentUserId();
			var isAllowed = User.IsInRole("Admin") ||
				id == userId ||
				await _context.MentorshipMatches.AnyAsync(match =>
					match.Status == "Active" &&
					match.StudentId == userId &&
					match.MentorId == id) ||
				await _context.TutoringAssignments.AnyAsync(assignment =>
					assignment.Status == "Active" &&
					assignment.StudentId == userId &&
					assignment.MentorId == id);
			if (!isAllowed)
			{
				return Forbid();
			}

			var mentor = await _context.Users
				.Include(u => u.MentorProfile)
				.Include(u => u.MentorMatches)
				.Where(u => u.UserId == id && u.MentorProfile != null)
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					u.Bio,
					u.AreasOfInterest,
					u.Availability,
					u.MentorProfile,
					CurrentStudents = u.MentorMatches.Count(m => m.Status == "Active")
				})
				.FirstOrDefaultAsync();

			return mentor == null ? NotFound() : Ok(mentor);
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
