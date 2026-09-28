using System.ComponentModel.DataAnnotations;

namespace MentoriaAPI.Models
{
	public class Role
	{
		[Key]
		public int RoleId { get; set; }

		[Required]
		[MaxLength(50)]
		public string RoleName { get; set; } = string.Empty;

		public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
	}
}