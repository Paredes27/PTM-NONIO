using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class User
	{
		[Key]
		public int UserId { get; set; }

		[Required]
		[MaxLength(100)]
		public string Name { get; set; } = string.Empty;

		[Required]
		[MaxLength(150)]
		public string Email { get; set; } = string.Empty;

		[Required]
		public string PasswordHash { get; set; } = string.Empty;

		public string? Bio { get; set; }
		[MaxLength(200)]
		public string AreasOfInterest { get; set; } = string.Empty;
		[MaxLength(200)]
		public string Availability { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
		public StudentProfile? StudentProfile { get; set; }
		public MentorProfile? MentorProfile { get; set; }
		public TutorProfile? TutorProfile { get; set; }
		public ICollection<MentorshipMatch> StudentMatches { get; set; } = new List<MentorshipMatch>();
		public ICollection<MentorshipMatch> MentorMatches { get; set; } = new List<MentorshipMatch>();
		public ICollection<TutoringAssignment> StudentTutoringAssignments { get; set; } = new List<TutoringAssignment>();
		public ICollection<TutoringAssignment> MentorTutoringAssignments { get; set; } = new List<TutoringAssignment>();
		public ICollection<TutoringAssignment> TutorAssignments { get; set; } = new List<TutoringAssignment>();
		public ICollection<MentorTutorAssignment> MentorTutorAssignments { get; set; } = new List<MentorTutorAssignment>();
		public ICollection<MentorTutorAssignment> TutorMentorAssignments { get; set; } = new List<MentorTutorAssignment>();
		public ICollection<Feedback> AuthoredFeedbacks { get; set; } = new List<Feedback>();
		public ICollection<Feedback> ReceivedFeedbacks { get; set; } = new List<Feedback>();
	}
}
