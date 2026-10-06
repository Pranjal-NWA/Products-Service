using ProductsService.DTOs;

namespace ProductsService.Services;

public interface IProductService
{
    Task<PagedProductsResponse> GetAllAsync(string? search, decimal? minPrice, decimal? maxPrice, int page, int pageSize);
    Task<ProductResponse> GetByIdAsync(Guid id);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
}