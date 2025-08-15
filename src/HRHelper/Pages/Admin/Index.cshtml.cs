using HRHelper.Data;
using HRHelper.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Pages.Admin
{
	[Authorize]
	public class IndexModel : PageModel
	{
		private readonly AppDbContext _db;
		private readonly IConfiguration _config;
		public List<SpecialRequest> Requests { get; set; } = new();
		public List<Submission> Submissions { get; set; } = new();

		public IndexModel(AppDbContext db, IConfiguration config)
		{
			_db = db;
			_config = config;
		}

		public async Task OnGet()
		{
			var reqs = await _db.SpecialRequests.AsNoTracking().ToListAsync();
			Requests = reqs.OrderByDescending(r => r.ExpiresAt).ToList();
			var subs = await _db.Submissions.Include(s => s.SpecialRequest).AsNoTracking().ToListAsync();
			Submissions = subs.OrderByDescending(s => s.SubmittedAt).Take(50).ToList();
		}

		public string GetLink(SpecialRequest r)
		{
			return r.Type switch
			{
				RequestType.Assignment => $"/Requests/Assignment/{r.Slug}",
				RequestType.EnglishVideo => $"/Requests/English/{r.Slug}",
				RequestType.Questionnaire => $"/Requests/Questionnaire/{r.Slug}",
				_ => "#"
			};
		}
	}
}
