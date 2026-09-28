namespace MentoriaAPI.Dtos
{
	public class CreateMatchRequest
	{
		[System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
		public int StudentId { get; set; }

		[System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
		public int MentorId { get; set; }
	}

	public class MatchRecommendationDto
	{
		public int MentorId { get; set; }
		public string MentorName { get; set; } = string.Empty;
		public string Course { get; set; } = string.Empty;
		public string Specialization { get; set; } = string.Empty;
		public decimal CompatibilityScore { get; set; }
		public string Summary { get; set; } = string.Empty;
	}
}
