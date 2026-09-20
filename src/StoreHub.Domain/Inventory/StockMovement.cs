using StoreHub.Domain.Catalog;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Inventory;

public sealed class StockMovement : AuditableEntity
{
    public Guid StoreId { get; set; }

    public Guid ProductId { get; set; }

    public StockMovementType MovementType { get; set; }

    public decimal QuantityChange { get; set; }

    public decimal QuantityAfter { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? Notes { get; set; }

    public Store Store { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
