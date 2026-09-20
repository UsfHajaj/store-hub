using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Notifications;

namespace StoreHub.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");

        builder.ConfigureAuditableDomainEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SubjectTemplate).HasMaxLength(512);
        builder.Property(x => x.BodyTemplate).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.NotificationType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Channel).HasConversion<byte>().IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        var deletedFilter = (byte)RecordStatus.Deleted;

        builder
            .HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter($"[{nameof(NotificationTemplate.RecordStatus)}] <> {deletedFilter}");
    }
}
