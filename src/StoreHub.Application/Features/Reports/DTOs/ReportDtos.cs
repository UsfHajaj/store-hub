namespace StoreHub.Application.Features.Reports.DTOs;

public sealed class ReportRangeRequest
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}

public sealed class NamedAmountDto
{
    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public int Count { get; init; }
}

public sealed class DailySalesPointDto
{
    public string Date { get; init; } = string.Empty;

    public decimal SalesTotal { get; init; }

    public int InvoiceCount { get; init; }
}

public sealed class HourlySalesPointDto
{
    public int Hour { get; init; }

    public decimal SalesTotal { get; init; }

    public int InvoiceCount { get; init; }
}

public sealed class CashierPerformanceDto
{
    public Guid? UserId { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string StoreNameAr { get; init; } = string.Empty;

    public string StoreNameEn { get; init; } = string.Empty;

    public decimal SalesTotal { get; init; }

    public int InvoiceCount { get; init; }

    public decimal AverageTicket { get; init; }

    public decimal ReturnsTotal { get; init; }

    public int ReturnCount { get; init; }
}

public sealed class InventoryStockItemDto
{
    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string CategoryNameAr { get; init; } = string.Empty;

    public string CategoryNameEn { get; init; } = string.Empty;

    public string StoreNameAr { get; init; } = string.Empty;

    public string StoreNameEn { get; init; } = string.Empty;

    public decimal StockQuantity { get; init; }

    public decimal ReorderLevel { get; init; }

    public bool IsLowStock { get; init; }
}

public sealed class RecentOrderDto
{
    public int InvoiceNumber { get; init; }

    public string StoreNameAr { get; init; } = string.Empty;

    public string StoreNameEn { get; init; } = string.Empty;

    public string CashierNameAr { get; init; } = string.Empty;

    public string CashierNameEn { get; init; } = string.Empty;

    public decimal GrandTotal { get; init; }

    public string PaymentMethodAr { get; init; } = string.Empty;

    public string PaymentMethodEn { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }
}

public sealed class StoreReportSummaryDto
{
    public Guid StoreId { get; init; }

    public string StoreNameAr { get; init; } = string.Empty;

    public string StoreNameEn { get; init; } = string.Empty;

    public decimal SalesTotal { get; init; }

    public decimal DiscountTotal { get; init; }

    public decimal ReturnsTotal { get; init; }

    public int InvoiceCount { get; init; }

    public int ReturnCount { get; init; }

    public decimal AverageTicket { get; init; }

    public int LowStockCount { get; init; }

    public int ProductCount { get; init; }

    public IReadOnlyList<DailySalesPointDto> DailySales { get; init; } = Array.Empty<DailySalesPointDto>();

    public IReadOnlyList<HourlySalesPointDto> HourlySales { get; init; } = Array.Empty<HourlySalesPointDto>();

    public IReadOnlyList<NamedAmountDto> TopProducts { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> SalesByCategory { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> SalesByCashier { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<CashierPerformanceDto> Cashiers { get; init; } = Array.Empty<CashierPerformanceDto>();

    public IReadOnlyList<NamedAmountDto> PaymentBreakdown { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> LowStockProducts { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> InventoryByCategory { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> TopProductsByUnits { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<InventoryStockItemDto> InventoryItems { get; init; } = Array.Empty<InventoryStockItemDto>();

    public IReadOnlyList<RecentOrderDto> RecentOrders { get; init; } = Array.Empty<RecentOrderDto>();
}

public sealed class AdminReportSummaryDto
{
    public decimal SalesTotal { get; init; }

    public decimal DiscountTotal { get; init; }

    public decimal ReturnsTotal { get; init; }

    public int InvoiceCount { get; init; }

    public int ReturnCount { get; init; }

    public int ActiveStoreCount { get; init; }

    public int LowStockCount { get; init; }

    public int ProductCount { get; init; }

    public IReadOnlyList<NamedAmountDto> SalesByStore { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<DailySalesPointDto> DailySales { get; init; } = Array.Empty<DailySalesPointDto>();

    public IReadOnlyList<HourlySalesPointDto> HourlySales { get; init; } = Array.Empty<HourlySalesPointDto>();

    public IReadOnlyList<NamedAmountDto> TopProducts { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> TopProductsByUnits { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> SalesByCategory { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> PaymentBreakdown { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> InventoryByCategory { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<NamedAmountDto> LowStockProducts { get; init; } = Array.Empty<NamedAmountDto>();

    public IReadOnlyList<CashierPerformanceDto> Cashiers { get; init; } = Array.Empty<CashierPerformanceDto>();

    public IReadOnlyList<InventoryStockItemDto> InventoryItems { get; init; } = Array.Empty<InventoryStockItemDto>();

    public IReadOnlyList<RecentOrderDto> RecentOrders { get; init; } = Array.Empty<RecentOrderDto>();

    public IReadOnlyList<StoreReportSummaryDto> Stores { get; init; } = Array.Empty<StoreReportSummaryDto>();
}
