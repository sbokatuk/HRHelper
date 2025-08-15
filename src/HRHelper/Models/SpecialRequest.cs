using System.ComponentModel.DataAnnotations;

namespace HRHelper.Models
{
	public class SpecialRequest
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[MaxLength(64)]
		public string Slug { get; set; } = string.Empty;

		[Required]
		public RequestType Type { get; set; }

		[Required]
		[MaxLength(256)]
		public string Title { get; set; } = string.Empty;

		[MaxLength(2000)]
		public string? Description { get; set; }

		public DateTimeOffset ExpiresAt { get; set; }

		// Type-specific settings as JSON (e.g., assignment fields, english prompt, questionnaire questions)
		public string PayloadJson { get; set; } = string.Empty;
	}
}
