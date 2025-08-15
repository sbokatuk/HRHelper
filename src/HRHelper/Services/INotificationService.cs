namespace HRHelper.Services
{
	public class SubmissionNotification
	{
		public string RequestTitle { get; set; } = string.Empty;
		public string RequestType { get; set; } = string.Empty;
		public string SubmittedAtIso { get; set; } = string.Empty;
		public string Summary { get; set; } = string.Empty;
	}

	public interface INotificationService
	{
		Task NotifyAsync(SubmissionNotification notification, CancellationToken ct = default);
	}
}
