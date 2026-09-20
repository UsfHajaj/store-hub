using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Catalog;

namespace StoreHub.Persistence.Configurations;

public sealed class ProductDiscountItemConfiguration : IEntityTypeConfiguration<ProductDiscountItem>
{
    public void Configure(EntityTypeBuilder<ProductDiscountItem> builder)
    {
        builder.ToTable("ProductDiscountItems");

        builder.HasKey(x => new { x.DiscountId, x.ProductId });

        builder.HasOne(x => x.Product)
            .WithMany(x => x.DiscountItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
