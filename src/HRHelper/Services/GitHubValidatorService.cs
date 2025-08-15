using System.Net.Http.Headers;
using System.Text.Json;

namespace HRHelper.Services
{
	public interface IGitHubValidatorService
	{
		Task<bool> IsPublicRepoNonEmptyAsync(string repoUrl, CancellationToken ct = default);
	}

	public class GitHubValidatorService : IGitHubValidatorService
	{
		private readonly HttpClient _httpClient;

		public GitHubValidatorService(IHttpClientFactory httpClientFactory)
		{
			_httpClient = httpClientFactory.CreateClient();
			_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HRHelper", "1.0"));
		}

		public async Task<bool> IsPublicRepoNonEmptyAsync(string repoUrl, CancellationToken ct = default)
		{
			try
			{
				var parts = repoUrl.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
				var owner = parts[^2];
				var repo = parts[^1].Replace(".git", string.Empty);
				var api = $"https://api.github.com/repos/{owner}/{repo}/commits?per_page=1";
				var resp = await _httpClient.GetAsync(api, ct);
				if (!resp.IsSuccessStatusCode) return false;
				var json = await resp.Content.ReadAsStringAsync(ct);
				return json.TrimStart().StartsWith("[") && !json.Contains("\"message\":\"Not Found\"");
			}
			catch
			{
				return false;
			}
		}
	}
}
