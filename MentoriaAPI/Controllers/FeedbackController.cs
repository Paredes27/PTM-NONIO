using MentoriaAPI.Data;
using MentoriaAPI.Dtos;
using MentoriaAPI.Models;
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
	public class FeedbackController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public FeedbackController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetFeedback()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var feedback = await _context.Feedbacks
				.Include(f => f.AuthorUser)
				.Include(f => f.TargetUser)
				.Include(f => f.Session)
				.ThenInclude(s => s.Match)
				.Where(f =>
					isAdmin ||
					f.AuthorUserId == userId ||
					f.TargetUserId == userId ||
					f.Session.Match.StudentId == userId ||
					f.Session.Match.MentorId == userId)
				.Select(f => new
				{
					f.FeedbackId,
					f.SessionId,
					f.AuthorUserId,
					f.TargetUserId,
					Author = f.AuthorUser.Name,
					Target = f.TargetUser.Name,
					f.Rating,
					f.Comment,
					f.CreatedAt
				})
				.ToListAsync();

			return Ok(feedback);
		}

		[HttpPost]
		public async Task<ActionResult> CreateFeedback(CreateFeedbackRequest request)
		{
			var userId = CurrentUserId();
			var session = await _context.MentoringSessions
				.Include(s => s.Match)
				.FirstOrDefaultAsync(s => s.SessionId == request.SessionId);
			if (session == null)
			{
				return NotFound("Sessão não encontrada.");
			}

			if (session.Status != "Completed")
			{
				return BadRequest("O feedback só pode ser registado em sessões concluídas.");
			}

			var participantIds = new[] { session.Match.StudentId, session.Match.MentorId };
			if (!participantIds.Contains(request.AuthorUserId) || !participantIds.Contains(request.TargetUserId))
			{
				return BadRequest("O autor e o destinatário do feedback devem pertencer ao emparelhamento da sessão.");
			}

			if (request.AuthorUserId == request.TargetUserId)
			{
				return BadRequest("O autor e o destinatário do feedback devem ser diferentes.");
			}

			if (!User.IsInRole("Admin") && request.AuthorUserId != userId)
			{
				return Forbid();
			}

			var duplicateFeedback = await _context.Feedbacks.AnyAsync(feedback =>
				feedback.SessionId == request.SessionId &&
				feedback.AuthorUserId == request.AuthorUserId);
			if (duplicateFeedback)
			{
				return Conflict("Este utilizador já registou feedback para esta sessão.");
			}

			var feedback = new Feedback
			{
				SessionId = request.SessionId,
				AuthorUserId = request.AuthorUserId,
				TargetUserId = request.TargetUserId,
				Rating = request.Rating,
				Comment = request.Comment
			};

			_context.Feedbacks.Add(feedback);
			await _context.SaveChangesAsync();
			return CreatedAtAction(nameof(GetFeedback), new { id = feedback.FeedbackId }, feedback);
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
