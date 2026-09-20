namespace StoreHub.Application.Features.Reports.DTOs;

public sealed class MyPerformanceDto
{
    public decimal TodaySales { get; init; }

    public int TodayInvoices { get; init; }

    public decimal TodayAverageTicket { get; init; }

    public decimal WeekSales { get; init; }

    public int WeekInvoices { get; init; }

    public decimal MonthSales { get; init; }

    public int MonthInvoices { get; init; }

    public decimal TodayReturns { get; init; }

    public int TodayReturnCount { get; init; }

    public string StoreNameAr { get; init; } = string.Empty;

    public string StoreNameEn { get; init; } = string.Empty;
}
