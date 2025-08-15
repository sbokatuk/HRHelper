using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Http.Json;

namespace HRHelper.Services
{
	public class CompositeNotificationService : INotificationService
	{
		private readonly IConfiguration _config;
		private readonly IHttpClientFactory _httpClientFactory;

		public CompositeNotificationService(IConfiguration config, IHttpClientFactory httpClientFactory)
		{
			_config = config;
			_httpClientFactory = httpClientFactory;
		}

		public async Task NotifyAsync(SubmissionNotification notification, CancellationToken ct = default)
		{
			var emailEnabled = _config.GetValue<bool>("Notifications:Email:Enabled");
			var telegramEnabled = _config.GetValue<bool>("Notifications:Telegram:Enabled");
			var tasks = new List<Task>();
			if (emailEnabled)
			{
				tasks.Add(SendEmailAsync(notification, ct));
			}
			if (telegramEnabled)
			{
				tasks.Add(SendTelegramAsync(notification, ct));
			}
			await Task.WhenAll(tasks);
		}

		private async Task SendEmailAsync(SubmissionNotification n, CancellationToken ct)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("HRHelper", _config["Notifications:Email:From"]));
			message.To.Add(new MailboxAddress("Admin", _config["Notifications:Email:To"]));
			message.Subject = $"HRHelper: новая отправка — {n.RequestTitle}";
			message.Body = new TextPart("plain")
			{
				Text = $"Тип: {n.RequestType}\nВремя: {n.SubmittedAtIso}\n{n.Summary}"
			};

			using var client = new SmtpClient();
			var host = _config["Notifications:Email:Host"] ?? string.Empty;
			var port = _config.GetValue<int>("Notifications:Email:Port");
			var useSsl = _config.GetValue<bool>("Notifications:Email:UseSsl");
			var username = _config["Notifications:Email:Username"] ?? string.Empty;
			var password = Environment.GetEnvironmentVariable(_config["Notifications:Email:PasswordEnvVar"] ?? string.Empty) ?? string.Empty;
			await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
			if (!string.IsNullOrEmpty(username))
			{
				await client.AuthenticateAsync(username, password, ct);
			}
			await client.SendAsync(message, ct);
			await client.DisconnectAsync(true, ct);
		}

		private async Task SendTelegramAsync(SubmissionNotification n, CancellationToken ct)
		{
			var token = Environment.GetEnvironmentVariable(_config["Notifications:Telegram:BotTokenEnvVar"] ?? string.Empty) ?? string.Empty;
			var chatId = _config["Notifications:Telegram:ChatId"] ?? string.Empty;
			if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId)) return;
			var text = $"HRHelper — новая отправка\nТип: {n.RequestType}\nЗаголовок: {n.RequestTitle}\nВремя: {n.SubmittedAtIso}\n{n.Summary}";
			var http = _httpClientFactory.CreateClient();
			var url = $"https://api.telegram.org/bot{token}/sendMessage";
			var payload = new { chat_id = chatId, text };
			await http.PostAsJsonAsync(url, payload, ct);
		}
	}
}
