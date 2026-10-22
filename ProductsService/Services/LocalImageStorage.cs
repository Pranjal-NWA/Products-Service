namespace ProductsService.Services;

public class LocalImageStorage : IImageStorage
{
    private readonly string _root;
    private readonly string _baseUrl;
    private const string Segment = "/uploads/products/";

    public LocalImageStorage(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Images:LocalPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "products")
            : configured;
        Directory.CreateDirectory(_root);
        _baseUrl = (config["Images:BaseUrl"] ?? "").TrimEnd('/');
    }

    public async Task<string> SaveAsync(Stream content, string extension, string contentType, CancellationToken ct)
    {
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var fs = File.Create(Path.Combine(_root, name));
        await content.CopyToAsync(fs, ct);
        return $"{_baseUrl}{Segment}{name}";
    }

    public Task DeleteAsync(string url, CancellationToken ct)
    {
        if (!url.Contains(Segment)) return Task.CompletedTask;
        var path = Path.Combine(_root, Path.GetFileName(url));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}