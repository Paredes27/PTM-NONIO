using MentoriaAPI.Data;
using MentoriaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MentoriaAPI.Controllers
{
	[ApiController]
	[Authorize(Roles = "Admin")]
	[Route("api/[controller]")]
	public class RolesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public RolesController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
		{
			return await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
		}
	}
}
