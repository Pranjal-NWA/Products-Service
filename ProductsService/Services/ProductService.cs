using System.Linq.Expressions;
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

    public ProductService(ProductsDbContext db) => _db = db;

    // One mapping used everywhere (queries and in-memory)
    private static readonly Expression<Func<Product, ProductResponse>> Map = p => new ProductResponse
    {
        Id = p.Id, Name = p.Name, Description = p.Description, Price = p.Price,
        ImageUrl = p.ImageUrl, Category = p.Category, Series = p.Series,
        SizeInch = p.SizeInch, Blades = p.Blades, Grade = p.Grade,
        IsFolding = p.IsFolding, LiftKg = p.LiftKg, MotorOdMm = p.MotorOdMm,
    };
    private static readonly Func<Product, ProductResponse> ToResponse = Map.Compile();

    public async Task<PagedProductsResponse> GetAllAsync(ProductQuery q)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = Math.Clamp(q.PageSize, 1, MaxPageSize);

        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = $"%{q.Search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, term) ||
                (p.Description != null && EF.Functions.ILike(p.Description, term)) ||
                (p.Series != null && EF.Functions.ILike(p.Series, term)));
        }
        if (q.Category  is { } cat)     query = query.Where(p => p.Category == cat);
        if (!string.IsNullOrWhiteSpace(q.Series))
        {
            var series = q.Series.Trim();
            query = query.Where(p => p.Series != null && EF.Functions.ILike(p.Series, series));
        }
        if (q.MinSize   is { } minSize) query = query.Where(p => p.SizeInch >= minSize);
        if (q.MaxSize   is { } maxSize) query = query.Where(p => p.SizeInch <= maxSize);
        if (q.Blades    is { } blades)  query = query.Where(p => p.Blades == blades);
        if (q.Grade     is { } grade)   query = query.Where(p => p.Grade == grade);
        if (q.Folding   is { } folding) query = query.Where(p => p.IsFolding == folding);
        if (q.MinLiftKg is { } lift)    query = query.Where(p => p.LiftKg >= lift);
        if (q.MotorOdMm is { } od)      query = query.Where(p => p.MotorOdMm == od);
        if (q.MinPrice  is { } lo)      query = query.Where(p => p.Price >= lo);
        if (q.MaxPrice  is { } hi)      query = query.Where(p => p.Price <= hi);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Category).ThenBy(p => p.Series).ThenBy(p => p.SizeInch)
            .ThenBy(p => p.LiftKg).ThenBy(p => p.MotorOdMm).ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(Map)
            .ToListAsync();

        return new PagedProductsResponse { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == id && p.IsActive)
            .Select(Map)
            .FirstOrDefaultAsync();

        if (product is null) throw new NotFoundApiException("Product not found");
        return product;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest r)
    {
        var product = new Product();
        Apply(product, r);
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest r)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) throw new NotFoundApiException("Product not found");

        Apply(product, r);
        product.IsActive = r.IsActive;
        await _db.SaveChangesAsync();
        return ToResponse(product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) throw new NotFoundApiException("Product not found");

        product.IsActive = false;   // soft delete
        await _db.SaveChangesAsync();
    }

    private static void Apply(Product p, CreateProductRequest r)
    {
        p.Name = r.Name;
        p.Description = r.Description;
        p.Price = r.Price;
        p.ImageUrl = r.ImageUrl;
        p.Category = r.Category;
        p.Series = r.Series;
        p.SizeInch = r.SizeInch;
        p.Blades = r.Blades;
        p.Grade = r.Grade;
        p.IsFolding = r.IsFolding;
        p.LiftKg = r.LiftKg;
        p.MotorOdMm = r.MotorOdMm;
    }
}