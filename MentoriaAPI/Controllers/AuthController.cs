using MentoriaAPI.Data;
using MentoriaAPI.Dtos;
using MentoriaAPI.Models;
using MentoriaAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MentoriaAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly PasswordService _passwordService;
		private readonly JwtTokenService _tokenService;

		public AuthController(ApplicationDbContext context, PasswordService passwordService, JwtTokenService tokenService)
		{
			_context = context;
			_passwordService = passwordService;
			_tokenService = tokenService;
		}

		[HttpPost("register")]
		public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
		{
			var normalizedRole = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role.Trim();

			if (await _context.Users.AnyAsync(u => u.Email == request.Email))
			{
				return Conflict("Já existe um utilizador com este email.");
			}

			var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == normalizedRole);
			if (role == null)
			{
				return BadRequest("Role inválida. Use Student, Mentor ou Admin.");
			}

			var user = new User
			{
				Name = request.Name,
				Email = request.Email,
				PasswordHash = _passwordService.HashPassword(request.Password),
				Bio = request.Bio,
				AreasOfInterest = request.AreasOfInterest,
				Availability = request.Availability
			};

			if (normalizedRole == "Student")
			{
				user.StudentProfile = new StudentProfile
				{
					Course = request.Course,
					AcademicYear = request.AcademicYear,
					Goals = request.Goals,
					AverageGrade = request.AverageGrade
				};
			}

			if (normalizedRole == "Mentor")
			{
				user.MentorProfile = new MentorProfile
				{
					Course = request.Course,
					Specialization = request.Specialization,
					AreasOfExpertise = request.AreasOfExpertise,
					Availability = request.Availability,
					MaxStudents = request.MaxStudents,
					YearsOfExperience = request.YearsOfExperience
				};
			}

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			_context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
			await _context.SaveChangesAsync();

			var token = _tokenService.GenerateToken(user, new[] { normalizedRole });
			return Ok(new AuthResponse
			{
				Token = token.Token,
				ExpiresAt = token.ExpiresAt,
				User = new
				{
					user.UserId,
					user.Name,
					user.Email,
					Role = normalizedRole
				}
			});
		}

		[HttpPost("login")]
		public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
		{
			var user = await _context.Users
				.Include(u => u.UserRoles)
				.ThenInclude(ur => ur.Role)
				.FirstOrDefaultAsync(u => u.Email == request.Email);

			if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
			{
				return Unauthorized("Credenciais inválidas.");
			}

			var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToArray();
			var token = _tokenService.GenerateToken(user, roles);

			return Ok(new AuthResponse
			{
				Token = token.Token,
				ExpiresAt = token.ExpiresAt,
				User = new
				{
					user.UserId,
					user.Name,
					user.Email,
					Roles = roles
				}
			});
		}
	}
}
