using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreHub.Domain.Identity;
using StoreHub.Domain.Stores;

namespace StoreHub.Persistence.Configurations;

public sealed class StoreMemberConfiguration : IEntityTypeConfiguration<StoreMember>
{
    public void Configure(EntityTypeBuilder<StoreMember> builder)
    {
        builder.ToTable("StoreMembers");

        builder.HasKey(x => new { x.StoreId, x.UserId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
    }
}
