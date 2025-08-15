using System.Text.Json;
using HRHelper.Data;
using HRHelper.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Pages.Admin
{
	[Authorize]
	public class TemplatesModel : PageModel
	{
		private readonly AppDbContext _db;
		public List<QuestionTemplate> Templates { get; set; } = new();
		public string? Message { get; set; }

		public TemplatesModel(AppDbContext db)
		{
			_db = db;
		}

		public async Task OnGet()
		{
			Templates = await _db.QuestionTemplates.OrderBy(t => t.Name).ToListAsync();
		}

		public async Task OnPost()
		{
			var idStr = Request.Form["Id"].ToString();
			var name = Request.Form["Name"].ToString();
			var qjson = Request.Form["QuestionsJson"].ToString();
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(qjson))
			{
				Message = "Имя и JSON обязательны";
				await OnGet();
				return;
			}
			if (!Guid.TryParse(idStr, out var id))
			{
				var t = new QuestionTemplate { Name = name, QuestionsJson = qjson };
				_db.QuestionTemplates.Add(t);
			}
			else
			{
				var ex = await _db.QuestionTemplates.FirstOrDefaultAsync(x => x.Id == id);
				if (ex == null)
				{
					_db.QuestionTemplates.Add(new QuestionTemplate { Id = id, Name = name, QuestionsJson = qjson });
				}
				else
				{
					ex.Name = name;
					ex.QuestionsJson = qjson;
				}
			}
			await _db.SaveChangesAsync();
			Message = "Сохранено";
			await OnGet();
		}
	}
}
