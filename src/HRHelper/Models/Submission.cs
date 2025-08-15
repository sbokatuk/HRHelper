using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRHelper.Models
{
	public class Submission
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid SpecialRequestId { get; set; }

		[ForeignKey(nameof(SpecialRequestId))]
		public SpecialRequest? SpecialRequest { get; set; }

		public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

		// JSON payload storing uploaded file paths, answers, links, etc.
		public string PayloadJson { get; set; } = string.Empty;
	}
}
