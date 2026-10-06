namespace ProductsService.DTOs;
using System.ComponentModel.DataAnnotations;


public class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}


public class CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
}

public class UpdateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

// Wraps the list response so the frontend knows total count without a
// separate call - standard shape for a paginated endpoint.
public class PagedProductsResponse
{
    public List<ProductResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}