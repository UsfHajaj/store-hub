using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Notifications;

namespace StoreHub.Persistence.Configurations;

public sealed class NotificationDispatchLogConfiguration : IEntityTypeConfiguration<NotificationDispatchLog>
{
    public void Configure(EntityTypeBuilder<NotificationDispatchLog> builder)
    {
        builder.ToTable("NotificationDispatchLogs");

        builder.ConfigureAuditableDomainEntity();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Channel).HasConversion<byte>().IsRequired();
        builder.Property(x => x.DispatchStatus).HasConversion<byte>().IsRequired();
        builder.Property(x => x.AttemptedOnUtc).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(512);
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);

        builder
            .HasOne(x => x.Notification)
            .WithMany(x => x.DispatchLogs)
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.NotificationId);
        builder.HasIndex(x => x.DispatchStatus);
        builder.HasIndex(x => x.AttemptedOnUtc);
    }
}
