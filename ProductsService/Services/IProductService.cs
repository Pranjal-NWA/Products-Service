using ProductsService.DTOs;
using ProductsService.Models;

namespace ProductsService.Services;

public interface IProductService
{
    // Browsing
    Task<PagedProductsResponse> GetAllAsync(ProductQuery query);
    Task<ProductResponse> GetByIdAsync(Guid id);
    Task<List<CategoryNode>> GetCategoryTreeAsync();
    Task<List<SeriesResponse>> GetSeriesAsync(ProductCategory? category, string? subcategory);
    Task<SeriesDetailResponse> GetSeriesByIdAsync(Guid id);

    // Admin: series
    Task<SeriesResponse> CreateSeriesAsync(SeriesRequest request);
    Task<SeriesResponse> UpdateSeriesAsync(Guid id, UpdateSeriesRequest request);
    Task DeleteSeriesAsync(Guid id);
    Task<SeriesResponse> UploadSeriesImageAsync(Guid id, Stream content, long length, CancellationToken ct);

    // Admin: orderable rows
    Task<ProductResponse> AddProductAsync(Guid seriesId, ProductRequest request);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task DeleteProductAsync(Guid id);
    Task<BulkCreateResult> BulkCreateAsync(List<BulkCreateProductRequest> requests);
}