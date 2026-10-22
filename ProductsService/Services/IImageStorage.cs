namespace ProductsService.Services;

public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, string extension, string contentType, CancellationToken ct);
    Task DeleteAsync(string url, CancellationToken ct);   // ignores URLs it doesn't own
}