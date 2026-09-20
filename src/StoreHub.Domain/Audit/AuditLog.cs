using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Audit;

/// <summary>Append-only record of domain changes (also used for simple workflow markers).</summary>
public sealed class AuditLog : BaseEntity
{
    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public Guid? PerformedByUserId { get; set; }

    public DateTime OccurredOnUtc { get; set; }

    public string? DetailsJson { get; set; }
}
