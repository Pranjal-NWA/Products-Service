namespace ProductsService.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Catalog fields (from the brochure)
    public ProductCategory Category { get; set; }
    public string? Series { get; set; }          // "LOFT Fixed", "VELD Series", "AXOM Heavy-lift"
    public int? SizeInch { get; set; }           // propellers
    public int? Blades { get; set; }             // 2 / 3
    public PropellerGrade? Grade { get; set; }   // UL / ST / HT
    public bool? IsFolding { get; set; }
    public int? LiftKg { get; set; }             // AXOM: 15–100
    public int? MotorOdMm { get; set; }          // motors: 88/117/132/150
}