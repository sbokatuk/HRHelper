using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace HRHelper.Models
{
	public class QuestionTemplate
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[MaxLength(128)]
		public string Name { get; set; } = string.Empty;

		// Serialized list of QuestionItem
		public string QuestionsJson { get; set; } = JsonSerializer.Serialize(new List<QuestionItem>());
	}

	public class QuestionItem
	{
		public string Id { get; set; } = Guid.NewGuid().ToString("N");
		public string Text { get; set; } = string.Empty;
		public bool Multiline { get; set; }
		public List<string>? Options { get; set; }
	}
}
