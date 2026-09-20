using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Identity;

namespace StoreHub.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("AppPermissions");

        builder.ConfigureAuditableEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(512);
        builder.Property(x => x.DescriptionEn).HasMaxLength(512);
        builder.Property(x => x.Module).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
