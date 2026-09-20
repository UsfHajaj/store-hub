using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Catalog;

namespace StoreHub.Persistence.Configurations;

public sealed class ProductDiscountConfiguration : IEntityTypeConfiguration<ProductDiscount>
{
    public void Configure(EntityTypeBuilder<ProductDiscount> builder)
    {
        builder.ToTable("ProductDiscounts");

        builder.ConfigureAuditableEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.AppliesToAllProducts).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CategoryId);

        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CategoryId);

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Discount)
            .HasForeignKey(x => x.DiscountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
