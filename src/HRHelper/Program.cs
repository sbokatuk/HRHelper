using HRHelper.Data;
using HRHelper.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Ensure we bind to the Cloud Run provided PORT when present
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portEnv))
{
	builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}

builder.Services.AddRazorPages();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllers().AddViewLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "ru", "ka", "uk" };
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=hrhelper.db";
// On Cloud Run, writeable path is /tmp. Use it for SQLite if using default.
if (!string.IsNullOrWhiteSpace(portEnv) && connectionString.Trim().Equals("Data Source=hrhelper.db", StringComparison.OrdinalIgnoreCase))
{
	connectionString = "Data Source=/tmp/hrhelper.db";
}
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddHttpClient();

var storageProvider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "Local";
var localUploadsRoot = builder.Configuration.GetValue<string>("Storage:Local:Root");
if (string.IsNullOrWhiteSpace(localUploadsRoot))
{
	// Default to temp dir in containerized environments
	localUploadsRoot = Path.Combine(Path.GetTempPath(), "uploads");
}
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
	builder.Services.AddSingleton<IStorageService>(sp => new LocalFileStorageService(localUploadsRoot));
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
Directory.CreateDirectory(localUploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(localUploadsRoot),
	RequestPath = "/uploads"
});

app.UseRouting();

// Persist culture via cookie if provided in query string
app.Use(async (context, next) =>
{
    var culture = context.Request.Query["culture"].ToString();
    if (!string.IsNullOrWhiteSpace(culture))
    {
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        context.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, cookieValue, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = false,
            Secure = context.Request.IsHttps
        });
    }
    await next();
});

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
