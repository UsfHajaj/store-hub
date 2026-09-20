using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Inventory;

namespace StoreHub.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.ConfigureAuditableEntity();
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovementType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.QuantityChange).HasPrecision(18, 3);
        builder.Property(x => x.QuantityAfter).HasPrecision(18, 3);
        builder.Property(x => x.ReferenceType).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(512);

        builder.HasIndex(x => new { x.StoreId, x.CreatedOnUtc });
        builder.HasIndex(x => x.ProductId);

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StocktakeConfiguration : IEntityTypeConfiguration<Stocktake>
{
    public void Configure(EntityTypeBuilder<Stocktake> builder)
    {
        builder.ToTable("Stocktakes");
        builder.ConfigureAuditableEntity();
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(512);

        builder.HasIndex(x => new { x.StoreId, x.CreatedOnUtc });

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Stocktake)
            .HasForeignKey(x => x.StocktakeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StocktakeLineConfiguration : IEntityTypeConfiguration<StocktakeLine>
{
    public void Configure(EntityTypeBuilder<StocktakeLine> builder)
    {
        builder.ToTable("StocktakeLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SystemQuantity).HasPrecision(18, 3);
        builder.Property(x => x.CountedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.Difference).HasPrecision(18, 3);

        builder.HasIndex(x => new { x.StocktakeId, x.ProductId }).IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
