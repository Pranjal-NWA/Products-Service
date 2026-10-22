using System.ComponentModel.DataAnnotations;
using ProductsService.Models;

namespace ProductsService.DTOs;

// ---------- Responses ----------

// One orderable row (one SKU), with its series info already joined in
public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }
    public string Series { get; set; } = string.Empty;      // "LOFT Fixed"
    public string Name { get; set; } = string.Empty;        // built: LOFT Fixed 24" HT
    public string Sku { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    public string? Subcategory { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? SizeInch { get; set; }
    public int? Blades { get; set; }
    public PropellerGrade? Grade { get; set; }
    public int? LiftKg { get; set; }
    public int? MotorOdMm { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; }
    public bool SeriesIsActive { get; set; }
}

public class PagedProductsResponse
{
    public List<ProductResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SeriesResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    public string? Subcategory { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
}

// A series plus the options that exist for it (for the product page pickers)
public class SeriesDetailResponse : SeriesResponse
{
    public List<int> SizesInch { get; set; } = new();
    public List<int> Blades { get; set; } = new();
    public List<PropellerGrade> Grades { get; set; } = new();
    public List<int> LiftKgOptions { get; set; } = new();
    public List<int> MotorOdMmOptions { get; set; } = new();
}

// Menu tree: Category -> Subcategory -> Series
public class SeriesSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class SubcategoryNode
{
    public string? Name { get; set; }
    public List<SeriesSummary> Series { get; set; } = new();
}

public class CategoryNode
{
    public ProductCategory Category { get; set; }
    public List<SubcategoryNode> Subcategories { get; set; } = new();
}

// ---------- Query ----------

public class ProductQuery
{
    public string? Search { get; set; }
    public ProductCategory? Category { get; set; }
    public string? Subcategory { get; set; }     // "Fixed", "Folding", ...
    public string? Series { get; set; }          // series name, e.g. "LOFT Fixed"
    public Guid? SeriesId { get; set; }
    public int? MinSize { get; set; }
    public int? MaxSize { get; set; }
    public int? Blades { get; set; }
    public PropellerGrade? Grade { get; set; }
    public int? MinLiftKg { get; set; }
    public int? MotorOdMm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public bool? IsActive { get; set; }          // admin only: true = available, false = unavailable
    public bool IncludeInactive { get; set; }    // admin only: true = show everything
}

// ---------- Admin requests ----------

public class SeriesRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    [MaxLength(50)] public string? Subcategory { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateSeriesRequest : SeriesRequest
{
    public bool IsActive { get; set; } = true;
}

public class ProductRequest
{
    [Required, MaxLength(60)] public string Sku { get; set; } = string.Empty;
    public int? SizeInch { get; set; }
    public int? Blades { get; set; }
    public PropellerGrade? Grade { get; set; }
    public int? LiftKg { get; set; }
    public int? MotorOdMm { get; set; }
    [Range(0, double.MaxValue)] public decimal? Price { get; set; }
}

public class UpdateProductRequest : ProductRequest
{
    public bool IsActive { get; set; } = true;
}

public class BulkCreateProductRequest
{
    [Required, MaxLength(100)] public string Series { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string SkuPrefix { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    [MaxLength(50)] public string? Subcategory { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    [Range(0, double.MaxValue)] public decimal? Price { get; set; }

    // Every list you fill in is multiplied together; leave unused ones out
    public List<int>? SizesInch { get; set; }
    public List<int>? Blades { get; set; }
    public List<PropellerGrade>? Grades { get; set; }
    public List<int>? LiftKgOptions { get; set; }
    public List<int>? MotorOdMmOptions { get; set; }
}

public class BulkCreateResult
{
    public int SeriesCreated { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
}