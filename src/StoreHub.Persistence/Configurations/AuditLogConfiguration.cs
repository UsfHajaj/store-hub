using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Audit;

namespace StoreHub.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EntityId);
        builder.Property(x => x.PerformedByUserId);
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.DetailsJson).HasMaxLength(8000);

        builder.HasIndex(x => x.OccurredOnUtc);
        builder.HasIndex(x => x.EntityType);
        builder.HasIndex(x => x.PerformedByUserId);

        builder.HasIndex(x => new { x.PerformedByUserId, x.OccurredOnUtc });
    }
}
