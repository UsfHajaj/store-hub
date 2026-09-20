using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Sales.DTOs;

public sealed class CreateSaleLineRequest
{
    public Guid ProductId { get; init; }

    public decimal Quantity { get; init; }
}

public sealed class CreateSaleRequest
{
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.Cash;

    public string? Notes { get; init; }

    public IReadOnlyList<CreateSaleLineRequest> Lines { get; init; } = Array.Empty<CreateSaleLineRequest>();
}

public sealed class SaleFilterRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}

public sealed class SaleListItemDto
{
    public Guid Id { get; init; }

    public int InvoiceNumber { get; init; }

    public Guid CashierUserId { get; init; }

    public string CashierName { get; init; } = string.Empty;

    public PaymentMethod PaymentMethod { get; init; }

    public SaleStatus Status { get; init; }

    public decimal Subtotal { get; init; }

    public decimal DiscountTotal { get; init; }

    public decimal GrandTotal { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public int LineCount { get; init; }
}

public sealed class SaleLineDto
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductNameAr { get; init; } = string.Empty;

    public string ProductNameEn { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal DiscountPercent { get; init; }

    public decimal LineSubtotal { get; init; }

    public decimal LineTotal { get; init; }

    public decimal ReturnedQuantity { get; init; }
}

public sealed class SaleDto
{
    public Guid Id { get; init; }

    public Guid StoreId { get; init; }

    public int InvoiceNumber { get; init; }

    public Guid CashierUserId { get; init; }

    public string CashierName { get; init; } = string.Empty;

    public PaymentMethod PaymentMethod { get; init; }

    public SaleStatus Status { get; init; }

    public decimal Subtotal { get; init; }

    public decimal DiscountTotal { get; init; }

    public decimal GrandTotal { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public IReadOnlyList<SaleLineDto> Lines { get; init; } = Array.Empty<SaleLineDto>();
}

public sealed class CreateSaleReturnLineRequest
{
    public Guid SaleLineId { get; init; }

    public decimal Quantity { get; init; }
}

public sealed class CreateSaleReturnRequest
{
    public string? Notes { get; init; }

    public IReadOnlyList<CreateSaleReturnLineRequest> Lines { get; init; } = Array.Empty<CreateSaleReturnLineRequest>();
}

public sealed class SaleReturnDto
{
    public Guid Id { get; init; }

    public Guid SaleId { get; init; }

    public int ReturnNumber { get; init; }

    public decimal GrandTotal { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
