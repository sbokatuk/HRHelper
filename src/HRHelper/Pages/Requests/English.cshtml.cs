using System.Text.Json;
using HRHelper.Data;
using HRHelper.Models;
using HRHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Pages.Requests
{
	public class EnglishPayload
	{
		public string? VideoUrl { get; set; }
		public string? Prompt { get; set; }
	}

	public class EnglishModel : PageModel
	{
		private readonly AppDbContext _db;
		private readonly IStorageService _storage;
        private readonly INotificationService _notify;

		public SpecialRequest? Special { get; set; }
		public EnglishPayload Payload { get; set; } = new();
		public string? Error { get; set; }
		public bool Success { get; set; }

		public EnglishModel(AppDbContext db, IStorageService storage, INotificationService notify)
		{
			_db = db;
			_storage = storage;
            _notify = notify;
		}

		public async Task<IActionResult> OnGetAsync(string slug)
		{
			Special = await _db.SpecialRequests.FirstOrDefaultAsync(r => r.Slug == slug && r.Type == RequestType.EnglishVideo);
			if (Special == null || Special.ExpiresAt < DateTimeOffset.UtcNow) return NotFound();
			Payload = JsonSerializer.Deserialize<EnglishPayload>(Special.PayloadJson) ?? new EnglishPayload();
			return Page();
		}

		public async Task<IActionResult> OnPostAsync(string slug)
		{
			Special = await _db.SpecialRequests.FirstOrDefaultAsync(r => r.Slug == slug && r.Type == RequestType.EnglishVideo);
			if (Special == null || Special.ExpiresAt < DateTimeOffset.UtcNow) return NotFound();
			Payload = JsonSerializer.Deserialize<EnglishPayload>(Special.PayloadJson) ?? new EnglishPayload();

			var file = Request.Form.Files["Video"];
			if (file == null || file.Length == 0)
			{
				Error = "Загрузите видео";
				return Page();
			}
			string saved;
			using (var s = file.OpenReadStream())
			{
				saved = await _storage.SaveAsync(s, file.ContentType, file.FileName);
			}
			var payload = new { VideoPath = saved };
			var submission = new Submission { SpecialRequestId = Special.Id, PayloadJson = JsonSerializer.Serialize(payload) };
			_db.Submissions.Add(submission);
			await _db.SaveChangesAsync();
			await _notify.NotifyAsync(new SubmissionNotification
			{
				RequestTitle = Special.Title,
				RequestType = nameof(RequestType.EnglishVideo),
				SubmittedAtIso = submission.SubmittedAt.UtcDateTime.ToString("u"),
				Summary = "Загружено видео"
			});
			Success = true;
			return Page();
		}
	}
}
