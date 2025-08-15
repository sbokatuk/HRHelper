using HRHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace HRHelper.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		public DbSet<SpecialRequest> SpecialRequests => Set<SpecialRequest>();
		public DbSet<Submission> Submissions => Set<Submission>();
		public DbSet<QuestionTemplate> QuestionTemplates => Set<QuestionTemplate>();
	}
}
