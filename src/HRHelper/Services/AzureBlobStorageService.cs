using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HRHelper.Services
{
	public class AzureBlobStorageService : IStorageService
	{
		private readonly BlobContainerClient _container;

		public AzureBlobStorageService(string connectionString, string container)
		{
			_container = new BlobContainerClient(connectionString, container);
			_container.CreateIfNotExists(PublicAccessType.None);
		}

		public async Task<string> SaveAsync(Stream content, string contentType, string fileName, CancellationToken ct = default)
		{
			var blobName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}_{fileName}";
			var blob = _container.GetBlobClient(blobName);
			await blob.UploadAsync(content, new BlobUploadOptions
			{
				HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
			}, ct);
			return blob.Uri.ToString();
		}
	}
}
