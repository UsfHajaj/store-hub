using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Sales.Interfaces;
using StoreHub.Domain.Enums;
using StoreHub.Persistence;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Infrastructure.Pdf;

public sealed class InvoicePdfService : IInvoicePdfService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;

    static InvoicePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InvoicePdfService(StoreHubDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<byte[]>> GenerateSaleInvoicePdfAsync(
        Guid storeId,
        Guid saleId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<byte[]>.Fail(access.Errors, access.FailureCode);
        }

        var sale = await _db.Sales.AsNoTracking()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (sale is null)
        {
            return Result<byte[]>.Fail("The sale was not found.", SalesErrors.SaleNotFound);
        }

        var store = await _db.Stores.AsNoTracking()
            .FirstAsync(s => s.Id == storeId, cancellationToken)
            .ConfigureAwait(false);

        var cashier = await _db.Users.AsNoTracking()
            .Where(u => u.Id == sale.CashierUserId)
            .Select(u => u.UserName ?? u.Email)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? "—";

        var displayName = !string.IsNullOrWhiteSpace(store.InvoiceDisplayNameAr)
            ? store.InvoiceDisplayNameAr
            : store.NameAr;
        var displayNameEn = !string.IsNullOrWhiteSpace(store.InvoiceDisplayNameEn)
            ? store.InvoiceDisplayNameEn
            : store.NameEn;

        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(store.LogoUrl))
        {
            logoBytes = TryReadLogo(store.LogoUrl);
        }

        var payment = sale.PaymentMethod == PaymentMethod.Cash ? "Cash / نقدي" : "Card / بطاقة";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    if (logoBytes is { Length: > 0 })
                    {
                        row.ConstantItem(72).Height(72).Image(logoBytes).FitArea();
                    }

                    row.RelativeItem().PaddingLeft(12).Column(col =>
                    {
                        col.Item().Text(displayName).FontSize(18).Bold();
                        col.Item().Text(displayNameEn).FontSize(12).FontColor(Colors.Grey.Darken1);
                        if (!string.IsNullOrWhiteSpace(store.TaxNumber))
                        {
                            col.Item().Text($"Tax / الرقم الضريبي: {store.TaxNumber}");
                        }
                    });

                    row.ConstantItem(140).AlignRight().Column(col =>
                    {
                        col.Item().Text("INVOICE / فاتورة").Bold().FontSize(14);
                        col.Item().Text($"#{sale.InvoiceNumber}");
                        col.Item().Text(sale.CreatedOnUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().Text($"Cashier / الكاشير: {cashier}");
                    col.Item().Text($"Payment / الدفع: {payment}");
                    col.Item().PaddingTop(12);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Item");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Qty");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Price");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Disc%");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Total");
                        });

                        foreach (var line in sale.Lines)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                .Text($"{line.ProductNameAr} / {line.ProductNameEn}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text(line.Quantity.ToString("0.###"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text($"LE {line.UnitPrice:0.00}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text(line.DiscountPercent.ToString("0.##"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight()
                                .Text($"LE {line.LineTotal:0.00}");
                        }
                    });

                    col.Item().AlignRight().PaddingTop(16).Column(tot =>
                    {
                        tot.Item().Text($"Subtotal: LE {sale.Subtotal:0.00}");
                        tot.Item().Text($"Discount: LE {sale.DiscountTotal:0.00}");
                        tot.Item().Text($"Grand Total: LE {sale.GrandTotal:0.00}").Bold().FontSize(14);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(store.InvoiceFooter ?? "Thank you / شكراً لزيارتكم");
                });
            });
        });

        var bytes = document.GeneratePdf();
        return Result<byte[]>.Ok(bytes);
    }

    private static byte[]? TryReadLogo(string logoUrl)
    {
        try
        {
            var relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var path = Path.Combine(webRoot, relative);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
        }
        catch
        {
            /* ignore logo errors */
        }

        return null;
    }
}
