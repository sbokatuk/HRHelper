using System.Text.Json;
using HRHelper.Data;
using HRHelper.Models;
using HRHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Pages.Requests
{
	public class AssignmentPayload
	{
		public string? VideoUrl { get; set; }
		public string? Prompt { get; set; }
		public string? PdfUrl { get; set; }
	}

	public class AssignmentModel : PageModel
	{
		private readonly AppDbContext _db;
		private readonly IStorageService _storage;
		private readonly IGitHubValidatorService _git;
        private readonly INotificationService _notify;

		public SpecialRequest? Special { get; set; }
		public AssignmentPayload Payload { get; set; } = new();
		public string? Error { get; set; }
		public bool Success { get; set; }

		public AssignmentModel(AppDbContext db, IStorageService storage, IGitHubValidatorService git, INotificationService notify)
		{
			_db = db;
			_storage = storage;
			_git = git;
            _notify = notify;
		}

		public async Task<IActionResult> OnGetAsync(string slug)
		{
			Special = await _db.SpecialRequests.FirstOrDefaultAsync(r => r.Slug == slug && r.Type == RequestType.Assignment);
			if (Special == null || Special.ExpiresAt < DateTimeOffset.UtcNow) return NotFound();
			Payload = JsonSerializer.Deserialize<AssignmentPayload>(Special.PayloadJson) ?? new AssignmentPayload();
			return Page();
		}

		public async Task<IActionResult> OnPostAsync(string slug)
		{
			Special = await _db.SpecialRequests.FirstOrDefaultAsync(r => r.Slug == slug && r.Type == RequestType.Assignment);
			if (Special == null || Special.ExpiresAt < DateTimeOffset.UtcNow) return NotFound();
			Payload = JsonSerializer.Deserialize<AssignmentPayload>(Special.PayloadJson) ?? new AssignmentPayload();

			var file = Request.Form.Files["Archive"];
			var gitUrl = Request.Form["GitUrl"].ToString();
			if (file == null && string.IsNullOrWhiteSpace(gitUrl))
			{
				Error = "Загрузите архив или укажите GitHub URL";
				return Page();
			}
			string uploadedPath = string.Empty;
			if (file != null)
			{
				if (file.Length == 0)
				{
					Error = "Пустой файл";
					return Page();
				}
				using var s = file.OpenReadStream();
				uploadedPath = await _storage.SaveAsync(s, file.ContentType, file.FileName);
			}
			bool repoOk = false;
			if (!string.IsNullOrWhiteSpace(gitUrl))
			{
				repoOk = await _git.IsPublicRepoNonEmptyAsync(gitUrl);
				if (!repoOk)
				{
					Error = "Репозиторий не найден или пуст";
					return Page();
				}
			}
			var payload = new { ArchivePath = string.IsNullOrEmpty(uploadedPath) ? null : uploadedPath, GitUrl = string.IsNullOrWhiteSpace(gitUrl) ? null : gitUrl };
			var submission = new Submission { SpecialRequestId = Special.Id, PayloadJson = JsonSerializer.Serialize(payload) };
			_db.Submissions.Add(submission);
			await _db.SaveChangesAsync();
			await _notify.NotifyAsync(new SubmissionNotification
			{
				RequestTitle = Special.Title,
				RequestType = nameof(RequestType.Assignment),
				SubmittedAtIso = submission.SubmittedAt.UtcDateTime.ToString("u"),
				Summary = string.IsNullOrWhiteSpace(gitUrl) ? "Загружен архив" : $"GitHub: {gitUrl}"
			});
			Success = true;
			return Page();
		}
	}
}
