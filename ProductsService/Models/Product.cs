namespace ProductsService.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SeriesId { get; set; }
    public ProductSeries Series { get; set; } = default!;

    public string Sku { get; set; } = string.Empty;        // "LOFT-F-24-HT"

    // Options: each is filled only when it applies to this series
    public int? SizeInch { get; set; }                     // propellers
    public int? Blades { get; set; }                       // 2 / 3
    public PropellerGrade? Grade { get; set; }             // UL / ST / HT
    public int? LiftKg { get; set; }                       // AXOM: 15–100
    public int? MotorOdMm { get; set; }                    // motors: 88/117/132/150

    public decimal? Price { get; set; }                    // null = "request quote"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}