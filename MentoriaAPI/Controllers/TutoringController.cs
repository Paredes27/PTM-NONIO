using MentoriaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MentoriaAPI.Controllers
{
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class TutoringController : ControllerBase
	{
		private readonly TutoringDistributionService _distributionService;

		public TutoringController(TutoringDistributionService distributionService)
		{
			_distributionService = distributionService;
		}

		[HttpGet("overview")]
		public async Task<ActionResult> GetOverview()
		{
			var isAdmin = User.IsInRole("Admin");
			var hideTutorDetails = User.IsInRole("Student") && !isAdmin;
			return Ok(await _distributionService.GetOverviewAsync(CurrentUserId(), isAdmin, hideTutorDetails));
		}

		[HttpPost("distribute/{phase:int}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult> Distribute(int phase)
		{
			if (phase is < 1 or > 3)
			{
				return BadRequest("A fase deve ser 1, 2 ou 3.");
			}

			await _distributionService.DistributeAsync(phase);
			return Ok(await _distributionService.GetOverviewAsync());
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
