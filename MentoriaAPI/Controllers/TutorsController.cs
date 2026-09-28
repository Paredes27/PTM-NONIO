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
	public class TutorsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public TutorsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetTutors()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var visibleTutorIds = isAdmin
				? new List<int>()
				: await _context.TutoringAssignments
					.Where(assignment =>
						assignment.Status == "Active" &&
						(assignment.StudentId == userId || assignment.MentorId == userId || assignment.TutorId == userId))
					.Select(assignment => assignment.TutorId)
					.Distinct()
					.ToListAsync();

			var tutors = await _context.Users
				.Include(u => u.TutorProfile)
				.Include(u => u.TutorAssignments)
				.Where(u => u.TutorProfile != null && (isAdmin || visibleTutorIds.Contains(u.UserId)))
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					Courses = u.TutorProfile!.Courses,
					u.TutorProfile.Department,
					u.TutorProfile.MaxMentors,
					CurrentAssignments = u.TutorAssignments.Count(a => a.Status == "Active")
				})
				.ToListAsync();

			return Ok(tutors);
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
