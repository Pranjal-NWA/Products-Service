namespace ProductsService.Models;

// One numbered item in the brochure (e.g. "LOFT Fixed"). Stored once.
public class ProductSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;       // "LOFT Fixed"
    public ProductCategory Category { get; set; }          // Propellers, Motors, ...
    public string? Subcategory { get; set; }               // "Fixed", "Folding", "iEPU", "Tubes", ...
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }                  // one photo for the whole line
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}