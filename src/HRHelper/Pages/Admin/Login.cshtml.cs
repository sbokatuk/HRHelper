using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HRHelper.Pages.Admin
{
	public class LoginModel : PageModel
	{
		[BindProperty]
		public string Password { get; set; } = string.Empty;

		public string? Error { get; set; }

		public void OnGet()
		{
		}

		public async Task<IActionResult> OnPostAsync()
		{
			var envVar = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<string>("Admin:PasswordEnvVar") ?? "ADMIN_PASSWORD";
			var expected = (Environment.GetEnvironmentVariable(envVar) ?? "admin").Trim();
			if (Password == expected)
			{
				var claims = new List<Claim> { new Claim(ClaimTypes.Name, "admin") };
				var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
				await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
				return RedirectToPage("/Admin/Index");
			}
			Error = "Неверный пароль";
			return Page();
		}
	}
}
