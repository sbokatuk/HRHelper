using Google.Cloud.Storage.V1;

namespace HRHelper.Services
{
	public class GcsStorageService : IStorageService
	{
		private readonly StorageClient _client = StorageClient.Create();
		private readonly string _bucket;

		public GcsStorageService(string bucket)
		{
			_bucket = bucket;
		}

		public async Task<string> SaveAsync(Stream content, string contentType, string fileName, CancellationToken ct = default)
		{
			var objectName = $"uploads/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}_{fileName}";
			await _client.UploadObjectAsync(_bucket, objectName, contentType, content, cancellationToken: ct);
			return $"gs://{_bucket}/{objectName}";
		}
	}
}
