using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class TutoringAssignment
	{
		[Key]
		public int AssignmentId { get; set; }

		public int StudentId { get; set; }
		public User Student { get; set; } = null!;

		public int? MentorId { get; set; }
		public User? Mentor { get; set; }

		public int TutorId { get; set; }
		public User Tutor { get; set; } = null!;

		[Required]
		[MaxLength(100)]
		public string Course { get; set; } = string.Empty;

		public int EnrollmentPhase { get; set; } = 1;

		[MaxLength(30)]
		public string Status { get; set; } = "Active";

		[MaxLength(250)]
		public string Summary { get; set; } = string.Empty;

		[MaxLength(800)]
		public string AllocationWarning { get; set; } = string.Empty;

		public bool MentorEmailSent { get; set; }
		public bool TutorEmailSent { get; set; }
		public bool NonioTutorListUpdated { get; set; }

		public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
	}
}
