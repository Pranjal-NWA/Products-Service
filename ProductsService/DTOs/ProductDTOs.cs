using System.ComponentModel.DataAnnotations;
using ProductsService.Models;

namespace ProductsService.DTOs;

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public ProductCategory Category { get; set; }
    public string? Series { get; set; }
    public int? SizeInch { get; set; }
    public int? Blades { get; set; }
    public PropellerGrade? Grade { get; set; }
    public bool? IsFolding { get; set; }
    public int? LiftKg { get; set; }
    public int? MotorOdMm { get; set; }
}

public class CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    public ProductCategory Category { get; set; }
    [MaxLength(100)] public string? Series { get; set; }
    public int? SizeInch { get; set; }
    public int? Blades { get; set; }
    public PropellerGrade? Grade { get; set; }
    public bool? IsFolding { get; set; }
    public int? LiftKg { get; set; }
    public int? MotorOdMm { get; set; }
}

public class UpdateProductRequest : CreateProductRequest
{
    public bool IsActive { get; set; } = true;
}

public class ProductQuery
{
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductCategory? Category { get; set; }
    public string? Series { get; set; }
    public int? MinSize { get; set; }
    public int? MaxSize { get; set; }
    public int? Blades { get; set; }
    public PropellerGrade? Grade { get; set; }
    public bool? Folding { get; set; }
    public int? MinLiftKg { get; set; }
    public int? MotorOdMm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedProductsResponse
{
    public List<ProductResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}