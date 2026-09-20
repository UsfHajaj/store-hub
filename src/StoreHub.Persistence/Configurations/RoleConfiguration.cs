using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Identity;

namespace StoreHub.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("AppRoles");

        builder.ConfigureAuditableEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(512);
        builder.Property(x => x.DescriptionEn).HasMaxLength(512);
        builder.Property(x => x.IsSystemRole).IsRequired();

        builder.HasIndex(x => x.NameEn).IsUnique();
    }
}
