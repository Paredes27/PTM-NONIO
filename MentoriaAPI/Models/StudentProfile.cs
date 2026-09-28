using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentoriaAPI.Models
{
	public class StudentProfile
	{
		[Key]
		[ForeignKey("User")]
		public int UserId { get; set; }

		[Required]
		[MaxLength(100)]
		public string Course { get; set; } = string.Empty;

		[Required]
		[MaxLength(20)]
		public string AcademicYear { get; set; } = string.Empty;
		[MaxLength(200)]
		public string Goals { get; set; } = string.Empty;
		public decimal AverageGrade { get; set; }
		public int EnrollmentPhase { get; set; } = 1;
		public bool IsEnrolled { get; set; } = true;

		public User User { get; set; } = null!;
	}
}
