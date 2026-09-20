using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Catalog;

namespace StoreHub.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.ConfigureAuditableEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(2000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(2000);
        builder.Property(x => x.Barcode).HasMaxLength(64);
        builder.Property(x => x.Sku).HasMaxLength(64);
        builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.StockQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.TracksInventory).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.ReorderLevel).HasPrecision(18, 3);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.StoreId, x.Barcode })
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [Barcode] <> ''");

        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CategoryId);

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
