namespace MentoriaAPI.Dtos
{
	public class CreateSessionRequest
	{
		[System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
		public int MatchId { get; set; }

		public DateTime ScheduledAt { get; set; }

		[System.ComponentModel.DataAnnotations.Range(15, 240)]
		public int DurationMinutes { get; set; }

		[System.ComponentModel.DataAnnotations.Required]
		[System.ComponentModel.DataAnnotations.MaxLength(120)]
		public string Topic { get; set; } = string.Empty;

		[System.ComponentModel.DataAnnotations.MaxLength(500)]
		public string Notes { get; set; } = string.Empty;
	}

	public class UpdateSessionStatusRequest
	{
		[System.ComponentModel.DataAnnotations.Required]
		[System.ComponentModel.DataAnnotations.RegularExpression("Scheduled|Completed|Cancelled")]
		public string Status { get; set; } = string.Empty;

		[System.ComponentModel.DataAnnotations.MaxLength(500)]
		public string Notes { get; set; } = string.Empty;
	}

	public class RescheduleSessionRequest
	{
		public DateTime ScheduledAt { get; set; }

		[System.ComponentModel.DataAnnotations.Range(15, 240)]
		public int DurationMinutes { get; set; }

		[System.ComponentModel.DataAnnotations.Required]
		[System.ComponentModel.DataAnnotations.MaxLength(120)]
		public string Topic { get; set; } = string.Empty;

		[System.ComponentModel.DataAnnotations.MaxLength(500)]
		public string Notes { get; set; } = string.Empty;
	}

	public class CreateFeedbackRequest
	{
		[System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
		public int SessionId { get; set; }

		[System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
		public int AuthorUserId { get; set; }

		[System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
		public int TargetUserId { get; set; }

		[System.ComponentModel.DataAnnotations.Range(1, 5)]
		public int Rating { get; set; }

		[System.ComponentModel.DataAnnotations.MaxLength(500)]
		public string Comment { get; set; } = string.Empty;
	}
}
