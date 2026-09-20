using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Inventory.Services;
using StoreHub.Application.Features.Reports.DTOs;
using StoreHub.Application.Features.Reports.Interfaces;
using StoreHub.Domain.Enums;
using StoreHub.Persistence;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Reports.Services;

public sealed class ReportService : IReportService
{
    private const int ChartTake = 12;
    private const int InventoryTake = 200;
    private const int RecentOrdersTake = 20;

    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReportService(StoreHubDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<StoreReportSummaryDto>> GetStoreSummaryAsync(
        Guid storeId,
        ReportRangeRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StoreReportSummaryDto>.Fail(access.Errors, access.FailureCode);
        }

        var (rangeFrom, rangeTo) = NormalizeRange(request);
        var dto = await BuildStoreSummaryAsync(storeId, rangeFrom, rangeTo, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return Result<StoreReportSummaryDto>.Fail("Store not found.", StoreErrors.StoreNotFound);
        }

        return Result<StoreReportSummaryDto>.Ok(dto);
    }

    public async Task<Result<AdminReportSummaryDto>> GetAdminSummaryAsync(
        ReportRangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var (rangeFrom, rangeTo) = NormalizeRange(request);
        var storeIds = await _db.Stores.AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stores = new List<StoreReportSummaryDto>();
        foreach (var id in storeIds)
        {
            var summary = await BuildStoreSummaryAsync(id, rangeFrom, rangeTo, cancellationToken).ConfigureAwait(false);
            if (summary is not null)
            {
                stores.Add(summary);
            }
        }

        var sales = await _db.Sales.AsNoTracking()
            .Where(s => s.CreatedOnUtc >= rangeFrom && s.CreatedOnUtc <= rangeTo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var returns = await _db.SaleReturns.AsNoTracking()
            .Where(r => r.CreatedOnUtc >= rangeFrom && r.CreatedOnUtc <= rangeTo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var daily = BuildDaily(sales);
        var hourly = BuildHourly(sales);

        var topProducts = await QueryTopProductsAsync(null, rangeFrom, rangeTo, byUnits: false, cancellationToken)
            .ConfigureAwait(false);
        var topByUnits = await QueryTopProductsAsync(null, rangeFrom, rangeTo, byUnits: true, cancellationToken)
            .ConfigureAwait(false);

        var byCategory = await (
                from l in _db.SaleLines.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on l.SaleId equals s.Id
                join p in _db.Products.AsNoTracking() on l.ProductId equals p.Id
                join c in _db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
                where s.CreatedOnUtc >= rangeFrom && s.CreatedOnUtc <= rangeTo
                group l by new { c.Id, c.NameAr, c.NameEn } into g
                orderby g.Sum(x => x.LineTotal) descending
                select new NamedAmountDto
                {
                    NameAr = g.Key.NameAr,
                    NameEn = g.Key.NameEn,
                    Amount = g.Sum(x => x.LineTotal),
                    Count = g.Count()
                })
            .Take(ChartTake)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryByCategory = await (
                from p in _db.Products.AsNoTracking()
                join c in _db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
                where p.IsActive && p.TracksInventory
                group p by new { c.Id, c.NameAr, c.NameEn } into g
                orderby g.Sum(x => x.StockQuantity) descending
                select new NamedAmountDto
                {
                    NameAr = g.Key.NameAr,
                    NameEn = g.Key.NameEn,
                    Amount = g.Sum(x => x.StockQuantity),
                    Count = g.Count()
                })
            .Take(ChartTake)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var productCount = await _db.Products.AsNoTracking()
            .CountAsync(p => p.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var payment = BuildPayment(sales);

        var inventoryRows = await (
                from p in _db.Products.AsNoTracking()
                join c in _db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
                join st in _db.Stores.AsNoTracking() on p.StoreId equals st.Id
                where p.IsActive && p.TracksInventory
                select new
                {
                    p.NameAr,
                    p.NameEn,
                    CatAr = c.NameAr,
                    CatEn = c.NameEn,
                    StoreAr = st.NameAr,
                    StoreEn = st.NameEn,
                    p.StockQuantity,
                    Reorder = p.ReorderLevel ?? InventoryService.DefaultReorderLevel
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryItems = inventoryRows
            .OrderBy(p => p.StockQuantity <= p.Reorder ? 0 : 1)
            .ThenBy(p => p.StockQuantity)
            .ThenBy(p => p.NameAr)
            .Take(InventoryTake)
            .Select(p => new InventoryStockItemDto
            {
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                CategoryNameAr = p.CatAr,
                CategoryNameEn = p.CatEn,
                StoreNameAr = p.StoreAr,
                StoreNameEn = p.StoreEn,
                StockQuantity = p.StockQuantity,
                ReorderLevel = p.Reorder,
                IsLowStock = p.StockQuantity <= p.Reorder
            })
            .ToList();

        var lowStockProducts = inventoryItems
            .Where(i => i.IsLowStock)
            .Take(ChartTake)
            .Select(i => new NamedAmountDto
            {
                NameAr = string.IsNullOrWhiteSpace(i.StoreNameAr) ? i.NameAr : $"{i.NameAr} — {i.StoreNameAr}",
                NameEn = string.IsNullOrWhiteSpace(i.StoreNameEn) ? i.NameEn : $"{i.NameEn} — {i.StoreNameEn}",
                Amount = i.StockQuantity,
                Count = 1
            })
            .ToList();

        var cashiers = await BuildCashiersAsync(storeId: null, rangeFrom, rangeTo, cancellationToken)
            .ConfigureAwait(false);
        var recentOrders = await BuildRecentOrdersAsync(storeId: null, rangeFrom, rangeTo, cancellationToken)
            .ConfigureAwait(false);

        return Result<AdminReportSummaryDto>.Ok(new AdminReportSummaryDto
        {
            SalesTotal = sales.Sum(s => s.GrandTotal),
            DiscountTotal = sales.Sum(s => s.DiscountTotal),
            ReturnsTotal = returns.Sum(r => r.GrandTotal),
            InvoiceCount = sales.Count,
            ReturnCount = returns.Count,
            ActiveStoreCount = storeIds.Count,
            LowStockCount = inventoryRows.Count(p => p.StockQuantity <= p.Reorder),
            ProductCount = productCount,
            SalesByStore = stores
                .OrderByDescending(s => s.SalesTotal)
                .Select(s => new NamedAmountDto
                {
                    NameAr = s.StoreNameAr,
                    NameEn = s.StoreNameEn,
                    Amount = s.SalesTotal,
                    Count = s.InvoiceCount
                })
                .ToList(),
            DailySales = daily,
            HourlySales = hourly,
            TopProducts = topProducts,
            TopProductsByUnits = topByUnits,
            SalesByCategory = byCategory,
            PaymentBreakdown = payment,
            InventoryByCategory = inventoryByCategory,
            LowStockProducts = lowStockProducts,
            Cashiers = cashiers,
            InventoryItems = inventoryItems,
            RecentOrders = recentOrders,
            Stores = stores
        });
    }

    public async Task<Result<MyPerformanceDto>> GetMyPerformanceAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null)
        {
            return Result<MyPerformanceDto>.Fail("Current user is required.", StoreErrors.CurrentUserRequired);
        }

        var access = await StoreAccessHelper.EnsureStoreAccessAsync(
                _db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<MyPerformanceDto>.Fail(access.Errors, access.FailureCode);
        }

        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => new { s.NameAr, s.NameEn })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (store is null)
        {
            return Result<MyPerformanceDto>.Fail("Store not found.", StoreErrors.StoreNotFound);
        }

        var userId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
        var monthStart = new DateTime(todayStart.Year, todayStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var mySales = await _db.Sales.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.CashierUserId == userId && s.CreatedOnUtc >= monthStart)
            .Select(s => new { s.CreatedOnUtc, s.GrandTotal })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var todaySales = mySales.Where(s => s.CreatedOnUtc >= todayStart).ToList();
        var weekSales = mySales.Where(s => s.CreatedOnUtc >= weekStart).ToList();

        var todayReturns = await (
                from r in _db.SaleReturns.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on r.SaleId equals s.Id
                where r.StoreId == storeId
                      && s.CashierUserId == userId
                      && r.CreatedOnUtc >= todayStart
                select r.GrandTotal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var todayTotal = todaySales.Sum(s => s.GrandTotal);
        return Result<MyPerformanceDto>.Ok(new MyPerformanceDto
        {
            TodaySales = todayTotal,
            TodayInvoices = todaySales.Count,
            TodayAverageTicket = todaySales.Count == 0 ? 0 : Math.Round(todayTotal / todaySales.Count, 2),
            WeekSales = weekSales.Sum(s => s.GrandTotal),
            WeekInvoices = weekSales.Count,
            MonthSales = mySales.Sum(s => s.GrandTotal),
            MonthInvoices = mySales.Count,
            TodayReturns = todayReturns.Sum(),
            TodayReturnCount = todayReturns.Count,
            StoreNameAr = store.NameAr,
            StoreNameEn = store.NameEn
        });
    }

    private async Task<StoreReportSummaryDto?> BuildStoreSummaryAsync(
        Guid storeId,
        DateTime rangeFrom,
        DateTime rangeTo,
        CancellationToken cancellationToken)
    {
        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => new { s.Id, s.NameAr, s.NameEn })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var sales = await _db.Sales.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.CreatedOnUtc >= rangeFrom && s.CreatedOnUtc <= rangeTo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var returns = await _db.SaleReturns.AsNoTracking()
            .Where(r => r.StoreId == storeId && r.CreatedOnUtc >= rangeFrom && r.CreatedOnUtc <= rangeTo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var daily = BuildDaily(sales);
        var hourly = BuildHourly(sales);

        var topProducts = await QueryTopProductsAsync(storeId, rangeFrom, rangeTo, byUnits: false, cancellationToken)
            .ConfigureAwait(false);
        var topByUnits = await QueryTopProductsAsync(storeId, rangeFrom, rangeTo, byUnits: true, cancellationToken)
            .ConfigureAwait(false);

        var byCategory = await (
                from l in _db.SaleLines.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on l.SaleId equals s.Id
                join p in _db.Products.AsNoTracking() on l.ProductId equals p.Id
                join c in _db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
                where s.StoreId == storeId && s.CreatedOnUtc >= rangeFrom && s.CreatedOnUtc <= rangeTo
                group l by new { c.Id, c.NameAr, c.NameEn } into g
                orderby g.Sum(x => x.LineTotal) descending
                select new NamedAmountDto
                {
                    NameAr = g.Key.NameAr,
                    NameEn = g.Key.NameEn,
                    Amount = g.Sum(x => x.LineTotal),
                    Count = g.Count()
                })
            .Take(ChartTake)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cashiers = await BuildCashiersAsync(storeId, rangeFrom, rangeTo, cancellationToken)
            .ConfigureAwait(false);
        var byCashier = cashiers
            .Select(c => new NamedAmountDto
            {
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                Amount = c.SalesTotal,
                Count = c.InvoiceCount
            })
            .ToList();

        var payment = BuildPayment(sales);

        var productRows = await (
                from p in _db.Products.AsNoTracking()
                join c in _db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
                where p.StoreId == storeId
                select new
                {
                    p.NameAr,
                    p.NameEn,
                    p.StockQuantity,
                    Reorder = p.ReorderLevel ?? InventoryService.DefaultReorderLevel,
                    p.IsActive,
                    p.TracksInventory,
                    CatAr = c.NameAr,
                    CatEn = c.NameEn,
                    CatId = c.Id
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var inventoryItems = productRows
            .Where(p => p.TracksInventory && p.IsActive)
            .OrderBy(p => p.StockQuantity <= p.Reorder ? 0 : 1)
            .ThenBy(p => p.StockQuantity)
            .ThenBy(p => p.NameAr)
            .Take(InventoryTake)
            .Select(p => new InventoryStockItemDto
            {
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                CategoryNameAr = p.CatAr,
                CategoryNameEn = p.CatEn,
                StoreNameAr = store.NameAr,
                StoreNameEn = store.NameEn,
                StockQuantity = p.StockQuantity,
                ReorderLevel = p.Reorder,
                IsLowStock = p.StockQuantity <= p.Reorder
            })
            .ToList();

        var low = inventoryItems
            .Where(p => p.IsLowStock)
            .Take(ChartTake)
            .Select(p => new NamedAmountDto
            {
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Amount = p.StockQuantity,
                Count = 1
            })
            .ToList();

        var inventoryByCategory = productRows
            .Where(p => p.TracksInventory && p.IsActive)
            .GroupBy(p => new { p.CatId, p.CatAr, p.CatEn })
            .OrderByDescending(g => g.Sum(x => x.StockQuantity))
            .Take(ChartTake)
            .Select(g => new NamedAmountDto
            {
                NameAr = g.Key.CatAr,
                NameEn = g.Key.CatEn,
                Amount = g.Sum(x => x.StockQuantity),
                Count = g.Count()
            })
            .ToList();

        var recentOrders = await BuildRecentOrdersAsync(storeId, rangeFrom, rangeTo, cancellationToken)
            .ConfigureAwait(false);

        var salesTotal = sales.Sum(s => s.GrandTotal);
        return new StoreReportSummaryDto
        {
            StoreId = store.Id,
            StoreNameAr = store.NameAr,
            StoreNameEn = store.NameEn,
            SalesTotal = salesTotal,
            DiscountTotal = sales.Sum(s => s.DiscountTotal),
            ReturnsTotal = returns.Sum(r => r.GrandTotal),
            InvoiceCount = sales.Count,
            ReturnCount = returns.Count,
            AverageTicket = sales.Count == 0 ? 0 : Math.Round(salesTotal / sales.Count, 2),
            LowStockCount = productRows.Count(p => p.TracksInventory && p.IsActive && p.StockQuantity <= p.Reorder),
            ProductCount = productRows.Count,
            DailySales = daily,
            HourlySales = hourly,
            TopProducts = topProducts,
            TopProductsByUnits = topByUnits,
            SalesByCategory = byCategory,
            SalesByCashier = byCashier,
            Cashiers = cashiers,
            PaymentBreakdown = payment,
            LowStockProducts = low,
            InventoryByCategory = inventoryByCategory,
            InventoryItems = inventoryItems,
            RecentOrders = recentOrders
        };
    }

    private async Task<List<CashierPerformanceDto>> BuildCashiersAsync(
        Guid? storeId,
        DateTime rangeFrom,
        DateTime rangeTo,
        CancellationToken cancellationToken)
    {
        var saleRows = await (
                from s in _db.Sales.AsNoTracking()
                join u in _db.Users.AsNoTracking() on s.CashierUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                join st in _db.Stores.AsNoTracking() on s.StoreId equals st.Id
                where s.CreatedOnUtc >= rangeFrom
                      && s.CreatedOnUtc <= rangeTo
                      && (storeId == null || s.StoreId == storeId)
                select new
                {
                    s.CashierUserId,
                    s.GrandTotal,
                    StoreAr = st.NameAr,
                    StoreEn = st.NameEn,
                    NameAr = u != null
                        ? (u.NameAr ?? u.UserName ?? u.Email ?? "?")
                        : "?",
                    NameEn = u != null
                        ? (u.NameEn ?? u.UserName ?? u.Email ?? "?")
                        : "?"
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var returnRows = await (
                from r in _db.SaleReturns.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on r.SaleId equals s.Id
                where r.CreatedOnUtc >= rangeFrom
                      && r.CreatedOnUtc <= rangeTo
                      && (storeId == null || r.StoreId == storeId)
                select new { s.CashierUserId, r.GrandTotal, StoreId = r.StoreId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return saleRows
            .GroupBy(x => new { x.CashierUserId, x.NameAr, x.NameEn, x.StoreAr, x.StoreEn })
            .Select(g =>
            {
                var total = g.Sum(x => x.GrandTotal);
                var count = g.Count();
                var userReturns = returnRows
                    .Where(r => r.CashierUserId == g.Key.CashierUserId)
                    .ToList();
                return new CashierPerformanceDto
                {
                    UserId = g.Key.CashierUserId,
                    NameAr = string.IsNullOrWhiteSpace(g.Key.NameAr) ? "?" : g.Key.NameAr,
                    NameEn = string.IsNullOrWhiteSpace(g.Key.NameEn) ? "?" : g.Key.NameEn,
                    StoreNameAr = g.Key.StoreAr,
                    StoreNameEn = g.Key.StoreEn,
                    SalesTotal = total,
                    InvoiceCount = count,
                    AverageTicket = count == 0 ? 0 : Math.Round(total / count, 2),
                    ReturnsTotal = userReturns.Sum(r => r.GrandTotal),
                    ReturnCount = userReturns.Count
                };
            })
            .OrderByDescending(c => c.SalesTotal)
            .ToList();
    }

    private async Task<List<RecentOrderDto>> BuildRecentOrdersAsync(
        Guid? storeId,
        DateTime rangeFrom,
        DateTime rangeTo,
        CancellationToken cancellationToken)
    {
        var rows = await (
                from s in _db.Sales.AsNoTracking()
                join u in _db.Users.AsNoTracking() on s.CashierUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                join st in _db.Stores.AsNoTracking() on s.StoreId equals st.Id
                where s.CreatedOnUtc >= rangeFrom
                      && s.CreatedOnUtc <= rangeTo
                      && (storeId == null || s.StoreId == storeId)
                orderby s.CreatedOnUtc descending
                select new
                {
                    s.InvoiceNumber,
                    s.GrandTotal,
                    s.PaymentMethod,
                    s.CreatedOnUtc,
                    StoreAr = st.NameAr,
                    StoreEn = st.NameEn,
                    CashierAr = u != null ? (u.NameAr ?? u.UserName ?? u.Email ?? "?") : "?",
                    CashierEn = u != null ? (u.NameEn ?? u.UserName ?? u.Email ?? "?") : "?"
                })
            .Take(RecentOrdersTake)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(r => new RecentOrderDto
        {
            InvoiceNumber = r.InvoiceNumber,
            StoreNameAr = r.StoreAr,
            StoreNameEn = r.StoreEn,
            CashierNameAr = r.CashierAr,
            CashierNameEn = r.CashierEn,
            GrandTotal = r.GrandTotal,
            PaymentMethodAr = r.PaymentMethod == PaymentMethod.Cash ? "نقدي" : "بطاقة",
            PaymentMethodEn = r.PaymentMethod.ToString(),
            CreatedOnUtc = r.CreatedOnUtc
        }).ToList();
    }

    private static List<DailySalesPointDto> BuildDaily(IReadOnlyList<Domain.Sales.Sale> sales) =>
        sales
            .GroupBy(s => DateOnly.FromDateTime(s.CreatedOnUtc))
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesPointDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                SalesTotal = g.Sum(x => x.GrandTotal),
                InvoiceCount = g.Count()
            })
            .ToList();

    private static List<HourlySalesPointDto> BuildHourly(IReadOnlyList<Domain.Sales.Sale> sales)
    {
        var byHour = sales
            .GroupBy(s => s.CreatedOnUtc.Hour)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                byHour.TryGetValue(h, out var list);
                list ??= [];
                return new HourlySalesPointDto
                {
                    Hour = h,
                    SalesTotal = list.Sum(x => x.GrandTotal),
                    InvoiceCount = list.Count
                };
            })
            .Where(h => h.InvoiceCount > 0)
            .ToList();
    }

    private static List<NamedAmountDto> BuildPayment(IReadOnlyList<Domain.Sales.Sale> sales) =>
        sales
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new NamedAmountDto
            {
                NameAr = g.Key == PaymentMethod.Cash ? "نقدي" : "بطاقة",
                NameEn = g.Key.ToString(),
                Amount = g.Sum(x => x.GrandTotal),
                Count = g.Count()
            })
            .ToList();

    private async Task<List<NamedAmountDto>> QueryTopProductsAsync(
        Guid? storeId,
        DateTime rangeFrom,
        DateTime rangeTo,
        bool byUnits,
        CancellationToken cancellationToken)
    {
        var rows = await (
                from l in _db.SaleLines.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on l.SaleId equals s.Id
                join p in _db.Products.AsNoTracking() on l.ProductId equals p.Id into pj
                from p in pj.DefaultIfEmpty()
                where s.CreatedOnUtc >= rangeFrom
                      && s.CreatedOnUtc <= rangeTo
                      && (storeId == null || s.StoreId == storeId)
                group new { l, p } by l.ProductId into g
                select new
                {
                    Amount = byUnits ? g.Sum(x => x.l.Quantity) : g.Sum(x => x.l.LineTotal),
                    Count = byUnits ? g.Count() : (int)g.Sum(x => x.l.Quantity),
                    ProductNameAr = g.Max(x => x.p != null ? x.p.NameAr : null),
                    ProductNameEn = g.Max(x => x.p != null ? x.p.NameEn : null),
                    LineNameAr = g.Max(x => x.l.ProductNameAr),
                    LineNameEn = g.Max(x => x.l.ProductNameEn),
                    Sku = g.Max(x => x.p != null ? x.p.Sku : null),
                    Barcode = g.Max(x => x.p != null ? x.p.Barcode : null)
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .OrderByDescending(x => x.Amount)
            .Take(ChartTake)
            .Select(x => new NamedAmountDto
            {
                NameAr = ResolveProductLabel(preferAr: true, x.ProductNameAr, x.LineNameAr, x.Sku, x.Barcode),
                NameEn = ResolveProductLabel(preferAr: false, x.ProductNameEn, x.LineNameEn, x.ProductNameAr, x.LineNameAr, x.Sku, x.Barcode),
                Amount = x.Amount,
                Count = x.Count
            })
            .ToList();
    }

    private static string ResolveProductLabel(bool preferAr, params string?[] candidates)
    {
        string? fallbackCode = null;
        foreach (var raw in candidates)
        {
            var t = raw?.Trim();
            if (string.IsNullOrWhiteSpace(t) || Guid.TryParse(t, out _))
            {
                continue;
            }

            if (t.All(char.IsDigit) && t.Length >= 6)
            {
                fallbackCode ??= t;
                continue;
            }

            return t;
        }

        if (!string.IsNullOrWhiteSpace(fallbackCode))
        {
            return fallbackCode;
        }

        return preferAr ? "منتج" : "Product";
    }

    private static (DateTime From, DateTime To) NormalizeRange(ReportRangeRequest request)
    {
        var to = request.ToUtc ?? DateTime.UtcNow;
        var from = request.FromUtc ?? to.AddDays(-30);
        if (from > to)
        {
            (from, to) = (to.AddDays(-30), from);
        }

        return (from, to);
    }
}
