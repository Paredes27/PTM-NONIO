using MentoriaAPI.Data;
using MentoriaAPI.Dtos;
using MentoriaAPI.Models;
using MentoriaAPI.Services;
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
	public class MatchesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly MatchingService _matchingService;

		public MatchesController(ApplicationDbContext context, MatchingService matchingService)
		{
			_context = context;
			_matchingService = matchingService;
		}

		[HttpGet]
		public async Task<ActionResult> GetMatches()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var matches = await _context.MentorshipMatches
				.Include(m => m.Student)
				.Include(m => m.Mentor)
				.Where(m => isAdmin || m.StudentId == userId || m.MentorId == userId)
				.Select(m => new
				{
					m.MatchId,
					m.StudentId,
					m.MentorId,
					Student = m.Student.Name,
					Mentor = m.Mentor.Name,
					m.CompatibilityScore,
					m.Status,
					m.Summary,
					m.CreatedAt
				})
				.ToListAsync();

			return Ok(matches);
		}

		[HttpGet("recommendations/{studentId:int}")]
		public async Task<ActionResult> GetRecommendations(int studentId)
		{
			if (!User.IsInRole("Admin") && studentId != CurrentUserId())
			{
				return Forbid();
			}

			var recommendations = await _matchingService.GetRecommendationsAsync(studentId);
			return Ok(recommendations);
		}

		[HttpPost("generate/{studentId:int}")]
		public async Task<ActionResult> GenerateBestMatch(int studentId)
		{
			if (!User.IsInRole("Admin") && studentId != CurrentUserId())
			{
				return Forbid();
			}

			var match = await _matchingService.CreateBestMatchAsync(studentId);
			if (match == null)
			{
				return NotFound("Não foi possível gerar emparelhamento.");
			}

			var result = await _context.MentorshipMatches
				.Include(m => m.Student)
				.Include(m => m.Mentor)
				.Where(m => m.MatchId == match.MatchId)
				.Select(m => new
					{
						m.MatchId,
						m.StudentId,
						m.MentorId,
						Student = m.Student.Name,
						Mentor = m.Mentor.Name,
						m.CompatibilityScore,
						m.Status,
						m.Summary,
						m.CreatedAt
					})
					.FirstAsync();

			return Ok(result);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult> CreateMatch(CreateMatchRequest request)
		{
			var studentExists = await _context.Users.AnyAsync(u => u.UserId == request.StudentId && u.StudentProfile != null);
			var mentorExists = await _context.Users.AnyAsync(u => u.UserId == request.MentorId && u.MentorProfile != null);
			if (!studentExists || !mentorExists)
			{
				return BadRequest("Aluno ou mentor inválido.");
			}

			var duplicateActiveMatch = await _context.MentorshipMatches
				.AnyAsync(m => m.StudentId == request.StudentId && m.MentorId == request.MentorId && m.Status == "Active");
			if (duplicateActiveMatch)
			{
				return Conflict("Já existe um emparelhamento ativo entre este aluno e mentor.");
			}

			var match = new MentorshipMatch
			{
				StudentId = request.StudentId,
				MentorId = request.MentorId,
				CompatibilityScore = 75,
				Status = "Active",
				Summary = "Emparelhamento manual."
			};

			_context.MentorshipMatches.Add(match);
			await _context.SaveChangesAsync();
			await _matchingService.SyncTutoringAssignmentAsync(match);

			var result = await _context.MentorshipMatches
				.Include(m => m.Student)
				.Include(m => m.Mentor)
				.Where(m => m.MatchId == match.MatchId)
				.Select(m => new
					{
						m.MatchId,
						m.StudentId,
						m.MentorId,
						Student = m.Student.Name,
						Mentor = m.Mentor.Name,
						m.CompatibilityScore,
						m.Status,
						m.Summary,
						m.CreatedAt
					})
					.FirstAsync();

			return CreatedAtAction(nameof(GetMatches), new { id = match.MatchId }, result);
		}

		[HttpGet("metrics")]
		public async Task<ActionResult> GetMetrics()
		{
			var userId = CurrentUserId();
			var isAdmin = User.IsInRole("Admin");
			var visibleMatches = _context.MentorshipMatches
				.Where(match => isAdmin || match.StudentId == userId || match.MentorId == userId);
			var visibleMatchIds = visibleMatches.Select(match => match.MatchId);
			var visibleSessions = _context.MentoringSessions
				.Where(session => visibleMatchIds.Contains(session.MatchId));
			var visibleSessionIds = visibleSessions.Select(session => session.SessionId);
			var visibleFeedback = _context.Feedbacks
				.Where(feedback =>
					visibleSessionIds.Contains(feedback.SessionId) &&
					(isAdmin || feedback.AuthorUserId == userId || feedback.TargetUserId == userId));
			var visibleTutoring = _context.TutoringAssignments
				.Where(assignment =>
					assignment.Status == "Active" &&
					(isAdmin || assignment.StudentId == userId || assignment.MentorId == userId || assignment.TutorId == userId));
			var totalMatches = await visibleMatches.CountAsync();
			var activeMatches = await visibleMatches.CountAsync(m => m.Status == "Active");
			var totalSessions = await visibleSessions.CountAsync();
			var averageFeedback = await visibleFeedback.AnyAsync()
				? await visibleFeedback.AverageAsync(f => f.Rating)
				: 0;

			return Ok(new
			{
				TotalUsers = isAdmin ? await _context.Users.CountAsync() : 1,
				TotalStudents = isAdmin ? await _context.StudentProfiles.CountAsync() : await _context.StudentProfiles.CountAsync(profile => profile.UserId == userId),
				TotalMentors = isAdmin ? await _context.MentorProfiles.CountAsync() : await _context.MentorProfiles.CountAsync(profile => profile.UserId == userId),
				TotalTutors = isAdmin ? await _context.TutorProfiles.CountAsync() : await _context.TutorProfiles.CountAsync(profile => profile.UserId == userId),
				TotalTutoringAssignments = await visibleTutoring.CountAsync(),
				AssignmentsWithoutMentor = await visibleTutoring.CountAsync(a => a.MentorId == null),
				TotalMatches = totalMatches,
				ActiveMatches = activeMatches,
				TotalSessions = totalSessions,
				AverageFeedback = Math.Round(averageFeedback, 2)
			});
		}

		private int CurrentUserId()
		{
			var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var userId) ? userId : 0;
		}
	}
}
