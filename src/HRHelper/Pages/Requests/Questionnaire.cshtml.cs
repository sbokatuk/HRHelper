using System.Text.Json;
using HRHelper.Data;
using HRHelper.Models;
using HRHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Pages.Requests
{
	public class QuestionnairePayload
	{
		public string? Prompt { get; set; }
		public string? TemplateId { get; set; }
		public List<QuestionItem>? Questions { get; set; }
	}

	public class QuestionnaireModel : PageModel
	{
		private readonly AppDbContext _db;
		private readonly INotificationService _notify;

		public SpecialRequest? Special { get; set; }
		public QuestionnairePayload Payload { get; set; } = new();
		public List<QuestionItem> Questions { get; set; } = new();
		public bool Success { get; set; }
		public string? Error { get; set; }

		public QuestionnaireModel(AppDbContext db, INotificationService notify)
		{
			_db = db;
			_notify = notify;
		}

		public async Task<IActionResult> OnGetAsync(string slug)
		{
			Special = await _db.SpecialRequests.FirstOrDefaultAsync(r => r.Slug == slug && r.Type == RequestType.Questionnaire);
			if (Special == null || Special.ExpiresAt < DateTimeOffset.UtcNow) return NotFound();
			Payload = JsonSerializer.Deserialize<QuestionnairePayload>(Special.PayloadJson) ?? new QuestionnairePayload();
			Questions = await ResolveQuestionsAsync(Payload);
			return Page();
		}

		public async Task<IActionResult> OnPostAsync(string slug)
		{
			Special = await _db.SpecialRequests.FirstOrDefaultAsync(r => r.Slug == slug && r.Type == RequestType.Questionnaire);
			if (Special == null || Special.ExpiresAt < DateTimeOffset.UtcNow) return NotFound();
			Payload = JsonSerializer.Deserialize<QuestionnairePayload>(Special.PayloadJson) ?? new QuestionnairePayload();
			Questions = await ResolveQuestionsAsync(Payload);

			var answers = new Dictionary<string, string>();
			foreach (var q in Questions)
			{
				var key = $"q_{q.Id}";
				answers[q.Id] = Request.Form[key].ToString();
			}
			var submission = new Models.Submission
			{
				SpecialRequestId = Special.Id,
				PayloadJson = JsonSerializer.Serialize(new { Answers = answers })
			};
			_db.Submissions.Add(submission);
			await _db.SaveChangesAsync();
			await _notify.NotifyAsync(new Services.SubmissionNotification
			{
				RequestTitle = Special.Title,
				RequestType = nameof(RequestType.Questionnaire),
				SubmittedAtIso = submission.SubmittedAt.UtcDateTime.ToString("u"),
				Summary = $"Ответов: {answers.Count}"
			});
			Success = true;
			return Page();
		}

		private async Task<List<QuestionItem>> ResolveQuestionsAsync(QuestionnairePayload payload)
		{
			if (!string.IsNullOrWhiteSpace(payload.TemplateId) && Guid.TryParse(payload.TemplateId, out var id))
			{
				var t = await _db.QuestionTemplates.FirstOrDefaultAsync(x => x.Id == id);
				if (t != null)
				{
					return System.Text.Json.JsonSerializer.Deserialize<List<QuestionItem>>(t.QuestionsJson) ?? new List<QuestionItem>();
				}
			}
			return payload.Questions ?? new List<QuestionItem>();
		}
	}
}
