using Microsoft.EntityFrameworkCore;
using StoreHub.Persistence;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Common;

public static class StoreAccessHelper
{
    public static async Task<Result> EnsureStoreAccessAsync(
        StoreHubDbContext db,
        Guid storeId,
        bool canManageAllStores,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (!await StoreExistsAsync(db, storeId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Fail("The store was not found.", StoreErrors.StoreNotFound);
        }

        if (canManageAllStores)
        {
            return Result.Ok();
        }

        if (userId is null)
        {
            return Result.Fail("Current user is required.", StoreErrors.CurrentUserRequired);
        }

        var isMember = await db.StoreMembers.AsNoTracking()
            .AnyAsync(m => m.StoreId == storeId && m.UserId == userId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (!isMember)
        {
            return Result.Fail("You do not have access to this store.", CatalogErrors.StoreAccessDenied);
        }

        return Result.Ok();
    }

    public static async Task<bool> StoreExistsAsync(
        StoreHubDbContext db,
        Guid storeId,
        CancellationToken cancellationToken = default) =>
        await db.Stores.AsNoTracking().AnyAsync(s => s.Id == storeId, cancellationToken).ConfigureAwait(false);
}
