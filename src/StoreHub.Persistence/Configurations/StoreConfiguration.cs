using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Stores;

namespace StoreHub.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");

        builder.ConfigureAuditableEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(512);
        builder.Property(x => x.DescriptionEn).HasMaxLength(512);
        builder.Property(x => x.StoreType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.OwnerUserId);

        builder.Property(x => x.InvoiceDisplayNameAr).HasMaxLength(128);
        builder.Property(x => x.InvoiceDisplayNameEn).HasMaxLength(128);
        builder.Property(x => x.LogoUrl).HasMaxLength(512);
        builder.Property(x => x.TaxNumber).HasMaxLength(64);
        builder.Property(x => x.InvoiceFooter).HasMaxLength(512);

        builder.HasIndex(x => x.NameEn);
        builder.HasIndex(x => x.IsActive);

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Store)
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
