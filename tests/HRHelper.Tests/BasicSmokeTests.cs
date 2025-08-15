using System.Net.Http;
using System.Text.Json;
using HRHelper.Services;
using Xunit;

namespace HRHelper.Tests
{
	public class BasicSmokeTests
	{
		[Fact]
		public void GitHubValidatorService_Constructs()
		{
			var factory = new HttpClientFactoryStub();
			var svc = new GitHubValidatorService(factory);
			Assert.NotNull(svc);
		}

		[Fact]
		public void NotificationModel_Serializes()
		{
			var n = new SubmissionNotification
			{
				RequestTitle = "Test",
				RequestType = "Assignment",
				SubmittedAtIso = "2025-01-01 00:00Z",
				Summary = "ok"
			};
			var json = JsonSerializer.Serialize(n);
			Assert.Contains("RequestTitle", json);
		}
	}

	internal class HttpClientFactoryStub : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => new HttpClient();
	}
}
