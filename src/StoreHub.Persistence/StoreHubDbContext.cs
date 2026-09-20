using System.Reflection;
using Microsoft.EntityFrameworkCore;
using StoreHub.Domain.Audit;
using StoreHub.Domain.Catalog;
using StoreHub.Domain.Common;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Identity;
using StoreHub.Domain.Inventory;
using StoreHub.Domain.Notifications;
using StoreHub.Domain.Sales;
using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;
using StoreHub.Shared.Identity;

namespace StoreHub.Persistence;

public sealed class StoreHubDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public StoreHubDbContext(DbContextOptions<StoreHubDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<NotificationDispatchLog> NotificationDispatchLogs => Set<NotificationDispatchLog>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<StoreMember> StoreMembers => Set<StoreMember>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductDiscount> ProductDiscounts => Set<ProductDiscount>();

    public DbSet<ProductDiscountItem> ProductDiscountItems => Set<ProductDiscountItem>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleLine> SaleLines => Set<SaleLine>();

    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();

    public DbSet<SaleReturnLine> SaleReturnLines => Set<SaleReturnLine>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();

    public DbSet<StocktakeLine> StocktakeLines => Set<StocktakeLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreHubDbContext).Assembly);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.Id == Guid.Empty)
                    {
                        entry.Entity.Id = Guid.CreateVersion7();
                    }

                    entry.Entity.CreatedOnUtc = utcNow;
                    entry.Entity.CreatedByUserId = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedOnUtc = utcNow;
                    entry.Entity.ModifiedByUserId = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.CreateVersion7();
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableDomainEntity>())
        {
            if (entry.State == EntityState.Modified &&
                entry.Entity.RecordStatus == RecordStatus.Deleted &&
                entry.Entity.DeletedOnUtc is null)
            {
                entry.Entity.DeletedOnUtc = utcNow;
                entry.Entity.DeletedByUserId = userId;
            }
        }

        // Audit log writes disabled — table grew too fast on shared hosting DB quotas.
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType is null || !typeof(AuditableDomainEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            var method = typeof(StoreHubDbContext)
                .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(clrType);

            method.Invoke(null, new object[] { modelBuilder });
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : AuditableDomainEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.RecordStatus != RecordStatus.Deleted);
    }
}
