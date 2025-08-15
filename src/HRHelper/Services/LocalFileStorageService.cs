namespace HRHelper.Services
{
	public class LocalFileStorageService : IStorageService
	{
		private readonly string _root;


		public LocalFileStorageService(string root)
		{
			_root = root;
			Directory.CreateDirectory(_root);
		}


		public async Task<string> SaveAsync(Stream content, string contentType, string fileName, CancellationToken ct = default)
		{
			var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
			var uniqueName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}_{safeName}";
			var fullPath = Path.Combine(_root, uniqueName);
			using (var fs = File.Create(fullPath))
			{
				await content.CopyToAsync(fs, ct);
			}
			// Return public relative URL that maps to /uploads in Program.cs
			return $"/uploads/{uniqueName}";
		}
	}
}
