using StoreHub.Domain.Catalog;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Sales;

public sealed class SaleReturnLine : BaseEntity
{
    public Guid SaleReturnId { get; set; }

    public Guid SaleLineId { get; set; }

    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public SaleReturn SaleReturn { get; set; } = null!;

    public SaleLine SaleLine { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
