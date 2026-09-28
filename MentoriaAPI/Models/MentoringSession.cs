using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class MentoringSession
	{
		[Key]
		public int SessionId { get; set; }

		public int MatchId { get; set; }
		public MentorshipMatch Match { get; set; } = null!;

		public DateTime ScheduledAt { get; set; }
		public int DurationMinutes { get; set; }

		[MaxLength(120)]
		public string Topic { get; set; } = string.Empty;

		[MaxLength(500)]
		public string Notes { get; set; } = string.Empty;

		[MaxLength(30)]
		public string Status { get; set; } = "Scheduled";

		public DateTime? CompletedAt { get; set; }

		public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
	}
}
