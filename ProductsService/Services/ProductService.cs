using Microsoft.EntityFrameworkCore;
using ProductsService.Data;
using ProductsService.DTOs;
using ProductsService.Exceptions;
using ProductsService.Models;

namespace ProductsService.Services;

public class ProductService : IProductService
{
    private readonly ProductsDbContext _db;
    private readonly IImageStorage _images;
    private const int MaxPageSize = 100;
    private const int MaxBulkRows = 500;
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public ProductService(ProductsDbContext db, IImageStorage images)
    {
        _db = db;
        _images = images;
    }

    // ===================== Browsing =====================

    public async Task<PagedProductsResponse> GetAllAsync(ProductQuery q)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = Math.Clamp(q.PageSize, 1, MaxPageSize);

        var query = _db.Products.AsNoTracking().AsQueryable();
        if (q.IsActive == false)
            query = query.Where(p => !p.IsActive || !p.Series.IsActive);   // unavailable
        else if (q.IsActive == true || !q.IncludeInactive)
            query = query.Where(p => p.IsActive && p.Series.IsActive);     // available (default)
                                                                           // else: IncludeInactive with no IsActive -> everything
        if (q.SeriesId is { } seriesId) query = query.Where(p => p.SeriesId == seriesId);
        if (q.Category is { } cat) query = query.Where(p => p.Series.Category == cat);
        if (!string.IsNullOrWhiteSpace(q.Subcategory))
        {
            var sub = q.Subcategory.Trim();
            query = query.Where(p => p.Series.Subcategory != null && EF.Functions.ILike(p.Series.Subcategory, sub));
        }
        if (!string.IsNullOrWhiteSpace(q.Series))
        {
            var name = q.Series.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Series.Name, name));
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = $"%{q.Search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Sku, term) ||
                EF.Functions.ILike(p.Series.Name, term) ||
                (p.Series.Description != null && EF.Functions.ILike(p.Series.Description, term)));
        }
        if (q.MinSize is { } minSize) query = query.Where(p => p.SizeInch >= minSize);
        if (q.MaxSize is { } maxSize) query = query.Where(p => p.SizeInch <= maxSize);
        if (q.Blades is { } blades) query = query.Where(p => p.Blades == blades);
        if (q.Grade is { } grade) query = query.Where(p => p.Grade == grade);
        if (q.MinLiftKg is { } lift) query = query.Where(p => p.LiftKg >= lift);
        if (q.MotorOdMm is { } od) query = query.Where(p => p.MotorOdMm == od);
        if (q.MinPrice is { } lo) query = query.Where(p => p.Price >= lo);
        if (q.MaxPrice is { } hi) query = query.Where(p => p.Price <= hi);

        var total = await query.CountAsync();

        var rows = await query
            .Include(p => p.Series)
            .OrderBy(p => p.Series.Category).ThenBy(p => p.Series.Name)
            .ThenBy(p => p.SizeInch).ThenBy(p => p.Blades).ThenBy(p => p.Grade)
            .ThenBy(p => p.LiftKg).ThenBy(p => p.MotorOdMm)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedProductsResponse
        {
            Items = rows.Select(ToResponse).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _db.Products.AsNoTracking().Include(p => p.Series)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.Series.IsActive);
        if (product is null) throw new NotFoundApiException("Product not found");
        return ToResponse(product);
    }

    public async Task<List<CategoryNode>> GetCategoryTreeAsync()
    {
        var rows = await _db.Series.AsNoTracking().Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.ImageUrl, s.Category, s.Subcategory })
            .ToListAsync();

        return rows.GroupBy(r => r.Category)
            .OrderBy(c => c.Key)
            .Select(c => new CategoryNode
            {
                Category = c.Key,
                Subcategories = c.GroupBy(r => r.Subcategory)
                    .OrderBy(s => s.Key)
                    .Select(s => new SubcategoryNode
                    {
                        Name = s.Key,
                        Series = s.Select(r => new SeriesSummary
                        {
                            Id = r.Id,
                            Name = r.Name,
                            ImageUrl = r.ImageUrl,
                        }).ToList(),
                    }).ToList(),
            }).ToList();
    }

    public async Task<List<SeriesResponse>> GetSeriesAsync(ProductCategory? category, string? subcategory)
    {
        var query = _db.Series.AsNoTracking().Where(s => s.IsActive);
        if (category is { } c) query = query.Where(s => s.Category == c);
        if (!string.IsNullOrWhiteSpace(subcategory))
        {
            var sub = subcategory.Trim();
            query = query.Where(s => s.Subcategory != null && EF.Functions.ILike(s.Subcategory, sub));
        }

        return await query.OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new SeriesResponse
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Subcategory = s.Subcategory,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                ProductCount = s.Products.Count(p => p.IsActive),
            }).ToListAsync();
    }

    public async Task<SeriesDetailResponse> GetSeriesByIdAsync(Guid id)
    {
        var detail = await _db.Series.AsNoTracking().Where(s => s.Id == id && s.IsActive)
            .Select(s => new SeriesDetailResponse
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Subcategory = s.Subcategory,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                ProductCount = s.Products.Count(p => p.IsActive),
            }).FirstOrDefaultAsync();
        if (detail is null) throw new NotFoundApiException("Series not found");

        var rows = _db.Products.AsNoTracking().Where(p => p.SeriesId == id && p.IsActive);
        detail.SizesInch = await rows.Where(p => p.SizeInch != null).Select(p => p.SizeInch!.Value).Distinct().OrderBy(x => x).ToListAsync();
        detail.Blades = await rows.Where(p => p.Blades != null).Select(p => p.Blades!.Value).Distinct().OrderBy(x => x).ToListAsync();
        detail.Grades = await rows.Where(p => p.Grade != null).Select(p => p.Grade!.Value).Distinct().ToListAsync();
        detail.LiftKgOptions = await rows.Where(p => p.LiftKg != null).Select(p => p.LiftKg!.Value).Distinct().OrderBy(x => x).ToListAsync();
        detail.MotorOdMmOptions = await rows.Where(p => p.MotorOdMm != null).Select(p => p.MotorOdMm!.Value).Distinct().OrderBy(x => x).ToListAsync();
        return detail;
    }

    // ===================== Admin: series =====================

    public async Task<SeriesResponse> CreateSeriesAsync(SeriesRequest r)
    {
        var name = r.Name.Trim();
        if (await _db.Series.AnyAsync(s => s.Name == name))
            throw new ConflictApiException("A series with this name already exists");

        var series = new ProductSeries
        {
            Name = name,
            Category = r.Category,
            Subcategory = r.Subcategory,
            Description = r.Description,
            ImageUrl = r.ImageUrl,
        };
        _db.Series.Add(series);
        await _db.SaveChangesAsync();
        return ToSeriesResponse(series, 0);
    }

    public async Task<SeriesResponse> UpdateSeriesAsync(Guid id, UpdateSeriesRequest r)
    {
        var series = await _db.Series.FirstOrDefaultAsync(s => s.Id == id);
        if (series is null) throw new NotFoundApiException("Series not found");

        var name = r.Name.Trim();
        if (name != series.Name && await _db.Series.AnyAsync(s => s.Name == name))
            throw new ConflictApiException("A series with this name already exists");

        series.Name = name;
        series.Category = r.Category;
        series.Subcategory = r.Subcategory;
        series.Description = r.Description;
        if (r.ImageUrl is not null) series.ImageUrl = r.ImageUrl;   // don't wipe an uploaded image
        series.IsActive = r.IsActive;
        await _db.SaveChangesAsync();

        return ToSeriesResponse(series, await CountActiveAsync(id));
    }

    public async Task DeleteSeriesAsync(Guid id)
    {
        var series = await _db.Series.FirstOrDefaultAsync(s => s.Id == id);
        if (series is null) throw new NotFoundApiException("Series not found");

        series.IsActive = false;   // soft delete: its products are hidden with it
        await _db.SaveChangesAsync();
    }

    public async Task<SeriesResponse> UploadSeriesImageAsync(Guid id, Stream content, long length, CancellationToken ct)
    {
        if (length <= 0 || length > MaxImageBytes)
            throw new BadRequestApiException("Image must be between 1 byte and 5 MB");

        var series = await _db.Series.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (series is null) throw new NotFoundApiException("Series not found");

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var detected = DetectImage(ms.ToArray());
        if (detected is null)
            throw new BadRequestApiException("Only JPEG, PNG or WebP images are allowed");

        ms.Position = 0;
        var oldUrl = series.ImageUrl;
        series.ImageUrl = await _images.SaveAsync(ms, detected.Value.Ext, detected.Value.ContentType, ct);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldUrl))
        {
            try { await _images.DeleteAsync(oldUrl, ct); }
            catch { /* best effort: an orphaned file must not fail the upload */ }
        }

        return ToSeriesResponse(series, await CountActiveAsync(id));
    }

    // ===================== Admin: orderable rows =====================

    public async Task<ProductResponse> AddProductAsync(Guid seriesId, ProductRequest r)
    {
        var series = await _db.Series.FirstOrDefaultAsync(s => s.Id == seriesId);
        if (series is null) throw new NotFoundApiException("Series not found");

        var sku = r.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Sku == sku))
            throw new ConflictApiException("A product with this SKU already exists");

        var product = new Product { Series = series, Sku = sku };
        Apply(product, r);
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest r)
    {
        var product = await _db.Products.Include(p => p.Series).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) throw new NotFoundApiException("Product not found");

        var sku = r.Sku.Trim().ToUpperInvariant();
        if (sku != product.Sku && await _db.Products.AnyAsync(p => p.Sku == sku))
            throw new ConflictApiException("A product with this SKU already exists");

        product.Sku = sku;
        Apply(product, r);
        product.IsActive = r.IsActive;
        await _db.SaveChangesAsync();
        return ToResponse(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) throw new NotFoundApiException("Product not found");

        product.IsActive = false;   // soft delete
        await _db.SaveChangesAsync();
    }

    public async Task<BulkCreateResult> BulkCreateAsync(List<BulkCreateProductRequest> requests)
    {
        var seriesByName = (await _db.Series.ToListAsync())
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        var existingSkus = (await _db.Products.Select(p => p.Sku).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seriesCreated = 0;
        var created = 0;
        var skipped = 0;

        foreach (var r in requests)
        {
            var sizes = Opts(r.SizesInch);
            var blades = Opts(r.Blades);
            var grades = Opts(r.Grades);
            var lifts = Opts(r.LiftKgOptions);
            var ods = Opts(r.MotorOdMmOptions);

            if (sizes.Count * blades.Count * grades.Count * lifts.Count * ods.Count > MaxBulkRows)
                throw new BadRequestApiException($"{r.Series}: too many combinations (max {MaxBulkRows})");

            var seriesName = r.Series.Trim();
            if (!seriesByName.TryGetValue(seriesName, out var series))
            {
                series = new ProductSeries
                {
                    Name = seriesName,
                    Category = r.Category,
                    Subcategory = r.Subcategory,
                    Description = r.Description,
                    ImageUrl = r.ImageUrl,
                };
                _db.Series.Add(series);
                seriesByName[seriesName] = series;
                seriesCreated++;
            }

            var prefix = r.SkuPrefix.Trim().ToUpperInvariant();

            foreach (var size in sizes)
                foreach (var b in blades)
                    foreach (var g in grades)
                        foreach (var kg in lifts)
                            foreach (var od in ods)
                            {
                                var skuParts = new List<string> { prefix };
                                if (size is { } s) skuParts.Add(s.ToString());
                                if (b is { } bl) skuParts.Add($"{bl}B");
                                if (g is { } gr) skuParts.Add(gr.ToString());
                                if (kg is { } k) skuParts.Add($"{k}KG");
                                if (od is { } o) skuParts.Add($"{o}MM");

                                var sku = string.Join("-", skuParts);
                                if (!existingSkus.Add(sku)) { skipped++; continue; }

                                _db.Products.Add(new Product
                                {
                                    Series = series,
                                    Sku = sku,
                                    Price = r.Price,
                                    SizeInch = size,
                                    Blades = b,
                                    Grade = g,
                                    LiftKg = kg,
                                    MotorOdMm = od,
                                });
                                created++;
                            }
        }

        await _db.SaveChangesAsync();
        return new BulkCreateResult { SeriesCreated = seriesCreated, Created = created, Skipped = skipped };
    }

    // ===================== Helpers =====================

    private static List<T?> Opts<T>(List<T>? list) where T : struct =>
        list is { Count: > 0 }
            ? list.Distinct().Select(x => (T?)x).ToList()
            : new List<T?> { null };

    private async Task<int> CountActiveAsync(Guid seriesId) =>
        await _db.Products.CountAsync(p => p.SeriesId == seriesId && p.IsActive);

    private static void Apply(Product p, ProductRequest r)
    {
        p.SizeInch = r.SizeInch;
        p.Blades = r.Blades;
        p.Grade = r.Grade;
        p.LiftKg = r.LiftKg;
        p.MotorOdMm = r.MotorOdMm;
        p.Price = r.Price;
    }

    private static SeriesResponse ToSeriesResponse(ProductSeries s, int count) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Category = s.Category,
        Subcategory = s.Subcategory,
        Description = s.Description,
        ImageUrl = s.ImageUrl,
        ProductCount = count,
    };

    // Series must be loaded on the product
    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id,
        SeriesId = p.SeriesId,
        Series = p.Series.Name,
        Name = DisplayName(p),
        Sku = p.Sku,
        Category = p.Series.Category,
        Subcategory = p.Series.Subcategory,
        Description = p.Series.Description,
        ImageUrl = p.Series.ImageUrl,
        SizeInch = p.SizeInch,
        Blades = p.Blades,
        Grade = p.Grade,
        LiftKg = p.LiftKg,
        MotorOdMm = p.MotorOdMm,
        Price = p.Price,
        IsActive = p.IsActive,
        SeriesIsActive = p.Series.IsActive,
    };

    // "LOFT Fixed" + 24 + HT  ->  LOFT Fixed 24" HT
    private static string DisplayName(Product p)
    {
        var parts = new List<string> { p.Series.Name };
        if (p.SizeInch is { } s) parts.Add($"{s}\"");
        if (p.Blades is { } b) parts.Add($"{b}-blade");
        if (p.Grade is { } g) parts.Add(g.ToString());
        if (p.LiftKg is { } k) parts.Add($"{k} kg");
        if (p.MotorOdMm is { } o) parts.Add($"{o} mm");
        return string.Join(" ", parts);
    }

    private static (string Ext, string ContentType)? DetectImage(byte[] b)
    {
        if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return (".jpg", "image/jpeg");
        if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            return (".png", "image/png");
        if (b.Length > 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            return (".webp", "image/webp");
        return null;
    }
}