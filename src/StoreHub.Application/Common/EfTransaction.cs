using Microsoft.EntityFrameworkCore;

namespace StoreHub.Application.Common;

public static class EfTransaction
{
    /// <summary>
    /// Runs work with an EF execution strategy so user transactions work with SQL retry.
    /// The <paramref name="operation"/> owns begin/commit/rollback of any transaction.
    /// </summary>
    public static Task<T> ExecuteAsync<T>(
        DbContext db,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(operation, cancellationToken);
    }
}
