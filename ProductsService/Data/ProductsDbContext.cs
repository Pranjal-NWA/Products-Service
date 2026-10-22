using Microsoft.EntityFrameworkCore;
using ProductsService.Models;

namespace ProductsService.Data;

public class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options) { }

    public DbSet<ProductSeries> Series => Set<ProductSeries>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductSeries>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(100);
            e.Property(s => s.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(s => s.Subcategory).HasMaxLength(50);
            e.HasIndex(s => s.Name).IsUnique();
            e.HasIndex(s => new { s.Category, s.Subcategory });
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Sku).HasMaxLength(60);
            e.Property(p => p.Grade).HasConversion<string>().HasMaxLength(2);
            e.Property(p => p.Price).HasPrecision(12, 2);
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => new { p.SeriesId, p.SizeInch });
            e.HasOne(p => p.Series).WithMany(s => s.Products)
             .HasForeignKey(p => p.SeriesId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}