using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Common;

namespace StoreHub.Persistence.Configurations;

internal static class AuditableEntityConfigurationExtensions
{
    public static void ConfigureAuditableDomainEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableDomainEntity
    {
        builder.Property(x => x.RecordStatus).HasConversion<byte>().IsRequired();
        builder.Property(x => x.DeletedOnUtc);
        builder.Property(x => x.DeletedByUserId);

        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedByUserId);
        builder.Property(x => x.ModifiedOnUtc);
        builder.Property(x => x.ModifiedByUserId);
    }
}
