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
	public class SessionsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public SessionsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<ActionResult> GetSessions()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			await CompleteElapsedSessionsAsync(userId, isAdmin);
			var sessions = await _context.MentoringSessions
				.Include(s => s.Match)
				.ThenInclude(m => m.Student)
				.Include(s => s.Match)
				.ThenInclude(m => m.Mentor)
				.Where(s => isAdmin || s.Match.StudentId == userId || s.Match.MentorId == userId)
				.Select(s => new
				{
					s.SessionId,
					s.MatchId,
					StudentId = s.Match.StudentId,
					MentorId = s.Match.MentorId,
					Student = s.Match.Student.Name,
					Mentor = s.Match.Mentor.Name,
					s.ScheduledAt,
					s.DurationMinutes,
					s.Topic,
					s.Status,
					s.Notes,
					s.CompletedAt
				})
				.ToListAsync();

			if (User.IsInRole("Student"))
			{
				var mentorIds = sessions.Select(session => session.MentorId).Distinct().ToList();
				var ownSessionIds = sessions.Select(session => session.SessionId).ToList();
				var blockedSlots = await _context.MentoringSessions
					.Include(s => s.Match)
					.ThenInclude(m => m.Mentor)
					.Where(s =>
						mentorIds.Contains(s.Match.MentorId) &&
						!ownSessionIds.Contains(s.SessionId) &&
						s.Status != "Completed")
					.Select(s => new
					{
						SessionId = 0,
						s.MatchId,
						StudentId = userId,
						MentorId = s.Match.MentorId,
						Student = "Outro aluno",
						Mentor = s.Match.Mentor.Name,
						s.ScheduledAt,
						s.DurationMinutes,
						Topic = "Mentor indisponível",
						Status = "Busy",
						Notes = "Horário ocupado por outra sessão do mentor.",
						CompletedAt = (DateTime?)null
					})
					.ToListAsync();

				return Ok(sessions.Concat(blockedSlots).OrderBy(session => session.ScheduledAt));
			}

			return Ok(sessions);
		}

		private async Task CompleteElapsedSessionsAsync(int userId, bool isAdmin)
		{
			var now = DateTime.UtcNow;
			var scheduledSessions = await _context.MentoringSessions
				.Include(session => session.Match)
				.Where(session =>
					session.Status == "Scheduled" &&
					(isAdmin || session.Match.StudentId == userId || session.Match.MentorId == userId))
				.ToListAsync();

			foreach (var session in scheduledSessions.Where(session => session.ScheduledAt.AddMinutes(session.DurationMinutes) <= now))
			{
				session.Status = "Completed";
				session.CompletedAt = session.ScheduledAt.AddMinutes(session.DurationMinutes);
				session.Notes = string.IsNullOrWhiteSpace(session.Notes)
					? "Sessão marcada automaticamente como concluída após o horário previsto."
					: $"{session.Notes} Sessão marcada automaticamente como concluída após o horário previsto.";
			}

			if (_context.ChangeTracker.HasChanges())
			{
				await _context.SaveChangesAsync();
			}
		}

		[HttpPost]
		public async Task<ActionResult> CreateSession(CreateSessionRequest request)
		{
			var userId = CurrentUserId();
			await CompleteElapsedSessionsAsync(userId, User.IsInRole("Admin"));
			var match = await _context.MentorshipMatches
				.Include(m => m.Student)
				.Include(m => m.Mentor)
				.FirstOrDefaultAsync(m => m.MatchId == request.MatchId && m.Status == "Active");
			if (match == null)
			{
				return NotFound("Emparelhamento ativo não encontrado.");
			}

			if (!User.IsInRole("Admin") && match.StudentId != userId && match.MentorId != userId)
			{
				return Forbid();
			}

			if (request.ScheduledAt <= DateTime.UtcNow.AddMinutes(-5))
			{
				return BadRequest("A sessão deve ser agendada para uma data futura.");
			}

			if (await HasScheduleConflictAsync(match.StudentId, match.MentorId, request.ScheduledAt, request.DurationMinutes))
			{
				return Conflict("Já existe uma sessão marcada nesse horário para este mentor ou aluno.");
			}

			var session = new MentoringSession
			{
				MatchId = request.MatchId,
				ScheduledAt = request.ScheduledAt,
				DurationMinutes = request.DurationMinutes,
				Topic = request.Topic,
				Notes = request.Notes,
				Status = "Scheduled"
			};

			_context.MentoringSessions.Add(session);
			await _context.SaveChangesAsync();
			return CreatedAtAction(nameof(GetSessions), new { id = session.SessionId }, session);
		}

		[HttpPut("{id:int}/reschedule")]
		public async Task<ActionResult> Reschedule(int id, RescheduleSessionRequest request)
		{
			var userId = CurrentUserId();
			await CompleteElapsedSessionsAsync(userId, User.IsInRole("Admin"));
			var session = await _context.MentoringSessions
				.Include(item => item.Match)
				.ThenInclude(match => match.Student)
				.Include(item => item.Match)
				.ThenInclude(match => match.Mentor)
				.FirstOrDefaultAsync(item => item.SessionId == id);
			if (session == null)
			{
				return NotFound();
			}

			if (!User.IsInRole("Admin") && session.Match.MentorId != userId)
			{
				return Forbid();
			}

			if (session.Status == "Completed")
			{
				return BadRequest("Sessões concluídas não podem ser remarcadas.");
			}

			if (request.ScheduledAt <= DateTime.UtcNow.AddMinutes(-5))
			{
				return BadRequest("A sessão deve ser remarcada para uma data futura.");
			}

			if (await HasScheduleConflictAsync(session.Match.StudentId, session.Match.MentorId, request.ScheduledAt, request.DurationMinutes, session.SessionId))
			{
				return Conflict("Já existe uma sessão marcada nesse horário para este mentor ou aluno.");
			}

			session.ScheduledAt = request.ScheduledAt;
			session.DurationMinutes = request.DurationMinutes;
			session.Topic = request.Topic;
			session.Notes = request.Notes;
			session.Status = "Scheduled";
			session.CompletedAt = null;

			await _context.SaveChangesAsync();
			return Ok(session);
		}

		[HttpPut("{id:int}/status")]
		public async Task<ActionResult> UpdateStatus(int id, UpdateSessionStatusRequest request)
		{
			var userId = CurrentUserId();
			var session = await _context.MentoringSessions
				.Include(item => item.Match)
				.FirstOrDefaultAsync(item => item.SessionId == id);
			if (session == null)
			{
				return NotFound();
			}

			if (!User.IsInRole("Admin") && session.Match.MentorId != userId)
			{
				return Forbid();
			}

			session.Status = request.Status;
			session.Notes = request.Notes;
			if (request.Status == "Completed")
			{
				session.CompletedAt = DateTime.UtcNow;
			}
			else
			{
				session.CompletedAt = null;
			}

			await _context.SaveChangesAsync();
			return Ok(session);
		}

		private async Task<bool> HasScheduleConflictAsync(
			int studentId,
			int mentorId,
			DateTime scheduledAt,
			int durationMinutes,
			int? excludedSessionId = null)
		{
			var scheduledEnd = scheduledAt.AddMinutes(durationMinutes);
			return await _context.MentoringSessions
				.Include(session => session.Match)
				.AnyAsync(session =>
					session.Status == "Scheduled" &&
					session.SessionId != excludedSessionId &&
					(session.Match.MentorId == mentorId || session.Match.StudentId == studentId) &&
					session.ScheduledAt < scheduledEnd &&
					scheduledAt < session.ScheduledAt.AddMinutes(session.DurationMinutes));
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
