using Microsoft.EntityFrameworkCore;
using ProductsService.Models;

namespace ProductsService.Data;

public class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.Grade).HasConversion<string>().HasMaxLength(2);
            e.Property(p => p.Series).HasMaxLength(100);
            e.HasIndex(p => p.Category);
            e.HasIndex(p => p.Series);
        });
    }
}