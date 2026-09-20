using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StoreHub.Domain.Audit;
using StoreHub.Persistence;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Middleware;

public sealed class RequestAuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestAuditLoggingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RequestAuditOptions _options;

    public RequestAuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestAuditLoggingMiddleware> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<RequestAuditOptions> options)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!_options.Enabled || !ShouldAudit(context))
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;
            var status = context.Response.StatusCode;
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var details = JsonSerializer.Serialize(new
            {
                method,
                path,
                query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                statusCode = status,
                traceId,
                userId = currentUser.UserId,
                userName = currentUser.UserName
            });

            var entityType = $"{method} {path}";
            if (entityType.Length > 256)
            {
                entityType = entityType[..256];
            }

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                Action = "Http.Request",
                EntityType = entityType,
                EntityId = null,
                PerformedByUserId = currentUser.UserId,
                OccurredOnUtc = DateTime.UtcNow,
                DetailsJson = details.Length > 8000 ? details[..8000] : details
            });

            await db.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist HTTP request audit for {Path}", context.Request.Path);
        }
    }

    private static bool ShouldAudit(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(ctx.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // High-frequency / low-value polling — do not clutter the audit log.
        if (path.Equals("/api/notifications/my/unread-count", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (path.EndsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

public sealed class RequestAuditOptions
{
    public const string SectionName = "RequestAudit";

    /// <summary>When false, HTTP request rows are not written (e.g. heavy load or tests).</summary>
    public bool Enabled { get; set; } = true;
}
