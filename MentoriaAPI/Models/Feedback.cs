using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class Feedback
	{
		[Key]
		public int FeedbackId { get; set; }

		public int SessionId { get; set; }
		public MentoringSession Session { get; set; } = null!;

		public int AuthorUserId { get; set; }
		public User AuthorUser { get; set; } = null!;

		public int TargetUserId { get; set; }
		public User TargetUser { get; set; } = null!;

		[Range(1, 5)]
		public int Rating { get; set; }

		[MaxLength(500)]
		public string Comment { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
