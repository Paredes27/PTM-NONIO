using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentoriaAPI.Models
{
	public class TutorProfile
	{
		[Key]
		[ForeignKey("User")]
		public int UserId { get; set; }

		[Required]
		[MaxLength(200)]
		public string Courses { get; set; } = string.Empty;

		[MaxLength(120)]
		public string Department { get; set; } = string.Empty;

		public int MaxMentors { get; set; } = 8;

		public User User { get; set; } = null!;
	}
}
