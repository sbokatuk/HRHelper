using System.Text.Json;
using HRHelper.Data;
using HRHelper.Models;
using HRHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRHelper.Pages.Admin
{
	[Authorize]
	public class CreateModel : PageModel
	{
		private readonly AppDbContext _db;
		private readonly IStorageService _storage;

		[BindProperty]
		public string Title { get; set; } = string.Empty;
		[BindProperty]
		public string? Description { get; set; }
		[BindProperty]
		public RequestType Type { get; set; }
		[BindProperty]
		public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);

		[BindProperty]
		public IFormFile? Pdf { get; set; }

		public string? CreatedLink { get; set; }
		public string? Error { get; set; }

		public CreateModel(AppDbContext db, IStorageService storage)
		{
			_db = db;
			_storage = storage;
		}

		public void OnGet()
		{
		}

		public async Task OnPost()
		{
			try
			{
				var slug = Guid.NewGuid().ToString("N").Substring(0, 10);
				var videoUrl = Request.Form["VideoUrl"].ToString();
				var prompt = Request.Form["Prompt"].ToString();
				var templateId = Request.Form["TemplateId"].ToString();
				string? pdfUrl = null;
				if (Pdf != null)
				{
					using var s = Pdf.OpenReadStream();
					pdfUrl = await _storage.SaveAsync(s, Pdf.ContentType, Pdf.FileName);
				}
				var payload = new
				{
					VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl,
					Prompt = prompt,
					PdfUrl = pdfUrl,
					TemplateId = string.IsNullOrWhiteSpace(templateId) ? null : templateId
				};
				var entity = new SpecialRequest
				{
					Slug = slug,
					Type = Type,
					Title = Title,
					Description = Description,
					ExpiresAt = ExpiresAt,
					PayloadJson = JsonSerializer.Serialize(payload)
				};
				_db.SpecialRequests.Add(entity);
				await _db.SaveChangesAsync();
				var link = Type switch
				{
					RequestType.Assignment => Url.Page("/Requests/Assignment", new { slug })!,
					RequestType.EnglishVideo => Url.Page("/Requests/English", new { slug })!,
					RequestType.Questionnaire => Url.Page("/Requests/Questionnaire", new { slug })!,
					_ => "#"
				};
				CreatedLink = link;
			}
			catch (Exception ex)
			{
				Error = ex.Message;
			}
		}
	}
}
