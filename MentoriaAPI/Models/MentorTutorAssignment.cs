using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class MentorTutorAssignment
	{
		[Key]
		public int AssignmentId { get; set; }

		public int MentorId { get; set; }
		public User Mentor { get; set; } = null!;

		public int TutorId { get; set; }
		public User Tutor { get; set; } = null!;

		[Required]
		[MaxLength(100)]
		public string Course { get; set; } = string.Empty;

		[MaxLength(30)]
		public string Status { get; set; } = "Active";

		[MaxLength(250)]
		public string Summary { get; set; } = string.Empty;

		public bool TutorEmailSent { get; set; }

		public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
	}
}
