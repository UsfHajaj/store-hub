using StoreHub.Domain.Enums;
using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Inventory;

public sealed class Stocktake : AuditableEntity
{
    public Guid StoreId { get; set; }

    public StocktakeStatus Status { get; set; } = StocktakeStatus.Draft;

    public Guid StartedByUserId { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public string? Notes { get; set; }

    public Store Store { get; set; } = null!;

    public ICollection<StocktakeLine> Lines { get; set; } = new List<StocktakeLine>();
}
