namespace StoreHub.Application.Features.AuditLogs.DTOs;

public sealed class AuditLogListItemDto
{
    public Guid Id { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public Guid? EntityId { get; init; }

    public Guid? PerformedByUserId { get; init; }

    public string? PerformedByUserName { get; init; }

    public string? PerformedByNameAr { get; init; }

    public string? PerformedByNameEn { get; init; }

    public string? EntityTargetUserName { get; set; }

    public string? EntityTargetNameAr { get; set; }

    public string? EntityTargetNameEn { get; set; }

    public DateTime OccurredOnUtc { get; init; }

    public string? DetailsJson { get; init; }
}
