using Microsoft.EntityFrameworkCore;
using ProductsService.Data;
using ProductsService.DTOs;
using ProductsService.Exceptions;
using ProductsService.Models;

namespace ProductsService.Services;

public class ProductService : IProductService
{
    private readonly ProductsDbContext _db;
    private const int MaxPageSize = 100;

    public ProductService(ProductsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedProductsResponse> GetAllAsync(
        string? search, decimal? minPrice, decimal? maxPrice, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? Math.Min(Math.Max(pageSize, 1), MaxPageSize) : pageSize;

        var query = _db.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
            })
            .ToListAsync();

        return new PagedProductsResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _db.Products
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
            })
            .FirstOrDefaultAsync();

        if (product is null)
            throw new NotFoundApiException("Product not found");

        return product;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
        };
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            throw new NotFoundApiException("Product not found");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            throw new NotFoundApiException("Product not found");

        // Soft delete - same reasoning as everywhere else in this
        // codebase (IsActive on ApplicationUser, IsRevoked on
        // RefreshToken rather than hard deletes): preserves history,
        // and a product referenced by a past order shouldn't vanish
        // out from under that order's record.
        product.IsActive = false;
        await _db.SaveChangesAsync();
    }
}