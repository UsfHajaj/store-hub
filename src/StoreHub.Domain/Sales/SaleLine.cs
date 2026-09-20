using StoreHub.Domain.Catalog;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Sales;

public sealed class SaleLine : BaseEntity
{
    public Guid SaleId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductNameAr { get; set; } = null!;

    public string ProductNameEn { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal LineSubtotal { get; set; }

    public decimal LineTotal { get; set; }

    public decimal ReturnedQuantity { get; set; }

    public Sale Sale { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
