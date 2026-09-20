using ClosedXML.Excel;
using StoreHub.Application.Features.Reports.DTOs;
using StoreHub.Application.Features.Reports.Interfaces;
using StoreHub.Shared.Results;

namespace StoreHub.Infrastructure.Excel;

public sealed class ReportExcelExportService : IReportExcelExportService
{
    private readonly IReportService _reportService;

    public ReportExcelExportService(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<Result<byte[]>> ExportStoreSummaryAsync(
        Guid storeId,
        ReportRangeRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetStoreSummaryAsync(storeId, request, canManageAllStores, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result<byte[]>.Fail(result.Errors, result.FailureCode);
        }

        var s = result.Value!;
        using var wb = new XLWorkbook();

        var summary = wb.Worksheets.Add("Summary");
        WritePairs(summary, new (string, object?)[]
        {
            ("StoreAr", s.StoreNameAr),
            ("StoreEn", s.StoreNameEn),
            ("SalesTotal", s.SalesTotal),
            ("DiscountTotal", s.DiscountTotal),
            ("ReturnsTotal", s.ReturnsTotal),
            ("InvoiceCount", s.InvoiceCount),
            ("ReturnCount", s.ReturnCount),
            ("AverageTicket", s.AverageTicket),
            ("ProductCount", s.ProductCount),
            ("LowStockCount", s.LowStockCount)
        });

        WriteNamed(wb.Worksheets.Add("DailySales"), "Date", "SalesTotal", "InvoiceCount",
            s.DailySales.Select(d => new object?[] { d.Date, d.SalesTotal, d.InvoiceCount }));
        WriteNamedAmount(wb.Worksheets.Add("TopProducts"), s.TopProducts);
        WriteNamedAmount(wb.Worksheets.Add("TopByUnits"), s.TopProductsByUnits);
        WriteNamedAmount(wb.Worksheets.Add("ByCategory"), s.SalesByCategory);
        WriteNamedAmount(wb.Worksheets.Add("ByCashier"), s.SalesByCashier);
        WriteNamedAmount(wb.Worksheets.Add("Payments"), s.PaymentBreakdown);
        WriteNamedAmount(wb.Worksheets.Add("InventoryByCategory"), s.InventoryByCategory);
        WriteNamedAmount(wb.Worksheets.Add("LowStock"), s.LowStockProducts);

        return Result<byte[]>.Ok(Save(wb));
    }

    public async Task<Result<byte[]>> ExportAdminSummaryAsync(
        ReportRangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetAdminSummaryAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result<byte[]>.Fail(result.Errors, result.FailureCode);
        }

        var a = result.Value!;
        using var wb = new XLWorkbook();

        var summary = wb.Worksheets.Add("Summary");
        WritePairs(summary, new (string, object?)[]
        {
            ("SalesTotal", a.SalesTotal),
            ("ReturnsTotal", a.ReturnsTotal),
            ("InvoiceCount", a.InvoiceCount),
            ("ReturnCount", a.ReturnCount),
            ("ActiveStoreCount", a.ActiveStoreCount),
            ("ProductCount", a.ProductCount),
            ("LowStockCount", a.LowStockCount)
        });

        WriteNamed(wb.Worksheets.Add("DailySales"), "Date", "SalesTotal", "InvoiceCount",
            a.DailySales.Select(d => new object?[] { d.Date, d.SalesTotal, d.InvoiceCount }));
        WriteNamedAmount(wb.Worksheets.Add("ByStore"), a.SalesByStore);
        WriteNamedAmount(wb.Worksheets.Add("TopProducts"), a.TopProducts);
        WriteNamedAmount(wb.Worksheets.Add("TopByUnits"), a.TopProductsByUnits);
        WriteNamedAmount(wb.Worksheets.Add("ByCategory"), a.SalesByCategory);
        WriteNamedAmount(wb.Worksheets.Add("Payments"), a.PaymentBreakdown);
        WriteNamedAmount(wb.Worksheets.Add("InventoryByCategory"), a.InventoryByCategory);

        return Result<byte[]>.Ok(Save(wb));
    }

    private static void WritePairs(IXLWorksheet ws, IEnumerable<(string Key, object? Value)> pairs)
    {
        ws.Cell(1, 1).Value = "Metric";
        ws.Cell(1, 2).Value = "Value";
        StyleHeader(ws, 2);
        var r = 2;
        foreach (var (key, value) in pairs)
        {
            ws.Cell(r, 1).Value = key;
            Set(ws.Cell(r, 2), value);
            r++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteNamedAmount(IXLWorksheet ws, IReadOnlyList<NamedAmountDto> items)
    {
        WriteNamed(ws, "NameAr", "NameEn", "Amount", "Count",
            items.Select(x => new object?[] { x.NameAr, x.NameEn, x.Amount, x.Count }));
    }

    private static void WriteNamed(
        IXLWorksheet ws,
        string h1,
        string h2,
        string h3,
        IEnumerable<object?[]> rows)
    {
        ws.Cell(1, 1).Value = h1;
        ws.Cell(1, 2).Value = h2;
        ws.Cell(1, 3).Value = h3;
        StyleHeader(ws, 3);
        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                Set(ws.Cell(r, c + 1), row[c]);
            }

            r++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteNamed(
        IXLWorksheet ws,
        string h1,
        string h2,
        string h3,
        string h4,
        IEnumerable<object?[]> rows)
    {
        ws.Cell(1, 1).Value = h1;
        ws.Cell(1, 2).Value = h2;
        ws.Cell(1, 3).Value = h3;
        ws.Cell(1, 4).Value = h4;
        StyleHeader(ws, 4);
        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                Set(ws.Cell(r, c + 1), row[c]);
            }

            r++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void StyleHeader(IXLWorksheet ws, int cols)
    {
        for (var c = 1; c <= cols; c++)
        {
            var cell = ws.Cell(1, c);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EC5B38");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void Set(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case string s:
                cell.Value = s;
                break;
            case int i:
                cell.Value = i;
                break;
            case decimal d:
                cell.Value = d;
                break;
            case double db:
                cell.Value = db;
                break;
            default:
                cell.Value = Convert.ToString(value);
                break;
        }
    }

    private static byte[] Save(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
