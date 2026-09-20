using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Identity;

namespace StoreHub.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("AppUsers");

        builder.ConfigureAuditableEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(128);
        builder.Property(x => x.NameEn).HasMaxLength(128);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.UserName).IsUnique();

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
