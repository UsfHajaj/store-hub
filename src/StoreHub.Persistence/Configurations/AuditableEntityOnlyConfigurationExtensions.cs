using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Persistence.Configurations;

internal static class AuditableEntityOnlyConfigurationExtensions
{
    public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedByUserId);
        builder.Property(x => x.ModifiedOnUtc);
        builder.Property(x => x.ModifiedByUserId);
    }
}
