using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class MentorshipMatch
	{
		[Key]
		public int MatchId { get; set; }

		public int StudentId { get; set; }
		public User Student { get; set; } = null!;

		public int MentorId { get; set; }
		public User Mentor { get; set; } = null!;

		public decimal CompatibilityScore { get; set; }

		[MaxLength(30)]
		public string Status { get; set; } = "Pending";

		[MaxLength(250)]
		public string Summary { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<MentoringSession> Sessions { get; set; } = new List<MentoringSession>();
	}
}
