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
	public class UsersController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public UsersController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetUsers()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var users = await _context.Users
				.Include(u => u.UserRoles)
				.ThenInclude(ur => ur.Role)
				.Include(u => u.StudentProfile)
				.Include(u => u.MentorProfile)
				.Where(u => isAdmin || u.UserId == userId)
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					u.Bio,
					u.AreasOfInterest,
					u.Availability,
					Roles = u.UserRoles.Select(ur => ur.Role.RoleName),
					StudentProfile = u.StudentProfile == null ? null : new
					{
						u.StudentProfile.Course,
						u.StudentProfile.AcademicYear,
						u.StudentProfile.Goals,
						u.StudentProfile.AverageGrade
					},
					MentorProfile = u.MentorProfile == null ? null : new
					{
						u.MentorProfile.Course,
						u.MentorProfile.Specialization,
						u.MentorProfile.AreasOfExpertise,
						u.MentorProfile.MaxStudents,
						u.MentorProfile.YearsOfExperience
					}
				})
				.ToListAsync();

			return Ok(users);
		}

		[HttpGet("{id:int}")]
		public async Task<ActionResult> GetUser(int id)
		{
			if (!User.IsInRole("Admin") && id != CurrentUserId())
			{
				return Forbid();
			}

			var user = await _context.Users
				.Include(u => u.UserRoles)
				.ThenInclude(ur => ur.Role)
				.Include(u => u.StudentProfile)
				.Include(u => u.MentorProfile)
				.Where(u => u.UserId == id)
				.Select(u => new
				{
					u.UserId,
					u.Name,
					u.Email,
					u.Bio,
					u.AreasOfInterest,
					u.Availability,
					u.IsActive,
					Roles = u.UserRoles.Select(ur => ur.Role.RoleName),
					StudentProfile = u.StudentProfile,
					MentorProfile = u.MentorProfile
				})
				.FirstOrDefaultAsync();

			return user == null ? NotFound() : Ok(user);
		}

		[HttpDelete("{id:int}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteUser(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			_context.Users.Remove(user);
			await _context.SaveChangesAsync();
			return NoContent();
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
