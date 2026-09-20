using StoreHub.Domain.Enums;
using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Sales;

public sealed class Sale : AuditableEntity
{
    public Guid StoreId { get; set; }

    public int InvoiceNumber { get; set; }

    public Guid CashierUserId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public Store Store { get; set; } = null!;

    public ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();

    public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
}
