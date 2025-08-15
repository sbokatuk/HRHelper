namespace HRHelper.Services
{
	public interface IStorageService
	{
		Task<string> SaveAsync(Stream content, string contentType, string fileName, CancellationToken ct = default);
	}
}
