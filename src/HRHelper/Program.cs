using HRHelper.Data;
using HRHelper.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=hrhelper.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddHttpClient();

var storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "Local";
if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
{
	builder.Services.AddSingleton<IStorageService>(sp => new AzureBlobStorageService(
		builder.Configuration.GetValue<string>("Storage:Azure:ConnectionString") ?? string.Empty,
		builder.Configuration.GetValue<string>("Storage:Azure:Container") ?? "uploads"));
}
else if (storageProvider.Equals("Gcs", StringComparison.OrdinalIgnoreCase))
{
	builder.Services.AddSingleton<IStorageService>(sp => new GcsStorageService(
		builder.Configuration.GetValue<string>("Storage:Gcs:Bucket") ?? string.Empty));
}
else
{
	builder.Services.AddSingleton<IStorageService>(sp => new LocalFileStorageService(
		Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads")));
}

builder.Services.AddSingleton<IGitHubValidatorService, GitHubValidatorService>();
builder.Services.AddSingleton<INotificationService, CompositeNotificationService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/admin/login";
		options.AccessDeniedPath = "/admin/login";
	});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	// Use EnsureCreated for zero-migration bootstrap to simplify setup
	db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Serve uploaded files if using local storage
var uploadsPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(uploadsPath),
	RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
