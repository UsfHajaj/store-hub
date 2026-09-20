using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Notifications;

namespace StoreHub.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.ConfigureAuditableDomainEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NotificationType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Title).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Channel).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.ReadOnUtc);
        builder.Property(x => x.RelatedEntityId);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(128);

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.NotificationType);
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
