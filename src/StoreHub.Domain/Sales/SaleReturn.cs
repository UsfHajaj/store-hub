using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Sales;

public sealed class SaleReturn : AuditableEntity
{
    public Guid StoreId { get; set; }

    public Guid SaleId { get; set; }

    public int ReturnNumber { get; set; }

    public Guid ProcessedByUserId { get; set; }

    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public Store Store { get; set; } = null!;

    public Sale Sale { get; set; } = null!;

    public ICollection<SaleReturnLine> Lines { get; set; } = new List<SaleReturnLine>();
}
