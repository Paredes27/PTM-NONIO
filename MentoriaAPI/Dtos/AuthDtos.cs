namespace MentoriaAPI.Dtos
{
	public class RegisterRequest
	{
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Role { get; set; } = "Student";
		public string Bio { get; set; } = string.Empty;
		public string AreasOfInterest { get; set; } = string.Empty;
		public string Availability { get; set; } = string.Empty;
		public string Course { get; set; } = string.Empty;
		public string AcademicYear { get; set; } = string.Empty;
		public string Goals { get; set; } = string.Empty;
		public decimal AverageGrade { get; set; }
		public string Specialization { get; set; } = string.Empty;
		public string AreasOfExpertise { get; set; } = string.Empty;
		public int MaxStudents { get; set; } = 5;
		public int YearsOfExperience { get; set; }
	}

	public class LoginRequest
	{
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class AuthResponse
	{
		public string Token { get; set; } = string.Empty;
		public DateTime ExpiresAt { get; set; }
		public object User { get; set; } = new();
	}
}
