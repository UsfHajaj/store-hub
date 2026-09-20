using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.AuditLogs.DTOs;

public sealed class AuditLogFilterRequest
{
    public int Page { get; set; } = PaginationConstants.DefaultPage;

    public int PageSize { get; set; } = PaginationConstants.DefaultPageSize;

    public string? EntityType { get; set; }

    public string? Action { get; set; }

    /// <summary>Free-text search in action, entity type, and JSON details (case-sensitive in SQL Server LIKE).</summary>
    public string? Search { get; set; }

    /// <summary>Inclusive lower bound (UTC).</summary>
    public DateTime? FromOccurredOnUtc { get; set; }

    /// <summary>Inclusive upper bound (UTC).</summary>
    public DateTime? ToOccurredOnUtc { get; set; }

    public Guid? PerformedByUserId { get; set; }
}
