using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentoriaAPI.Models
{
	public class MentorProfile
	{
		[Key]
		[ForeignKey("User")]
		public int UserId { get; set; }

		[MaxLength(100)]
		public string Specialization { get; set; } = string.Empty;
		[MaxLength(100)]
		public string Course { get; set; } = string.Empty;
		[MaxLength(200)]
		public string AreasOfExpertise { get; set; } = string.Empty;
		[MaxLength(100)]
		public string Availability { get; set; } = string.Empty;
		public int YearsOfExperience { get; set; }
		public int AcademicYear { get; set; } = 2;

		public int MaxStudents { get; set; }

		public User User { get; set; } = null!;
	}
}
