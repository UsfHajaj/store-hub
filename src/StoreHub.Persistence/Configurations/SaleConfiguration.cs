using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Sales;

namespace StoreHub.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.ConfigureAuditableEntity();
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(512);

        builder.HasIndex(x => new { x.StoreId, x.InvoiceNumber }).IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.CreatedOnUtc });

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Sale)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Returns)
            .WithOne(x => x.Sale)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductNameAr).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProductNameEn).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountPercent).HasPrecision(9, 2);
        builder.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Property(x => x.ReturnedQuantity).HasPrecision(18, 3);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("SaleReturns");
        builder.ConfigureAuditableEntity();
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnNumber).IsRequired();
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(512);

        builder.HasIndex(x => new { x.StoreId, x.ReturnNumber }).IsUnique();

        builder.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.SaleReturn)
            .HasForeignKey(x => x.SaleReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SaleReturnLineConfiguration : IEntityTypeConfiguration<SaleReturnLine>
{
    public void Configure(EntityTypeBuilder<SaleReturnLine> builder)
    {
        builder.ToTable("SaleReturnLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);

        builder.HasOne(x => x.SaleLine)
            .WithMany()
            .HasForeignKey(x => x.SaleLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
