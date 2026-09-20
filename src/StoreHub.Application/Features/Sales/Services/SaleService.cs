using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Sales.DTOs;
using StoreHub.Application.Features.Sales.Interfaces;
using StoreHub.Domain.Catalog;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Inventory;
using StoreHub.Domain.Sales;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Sales.Services;

public sealed class SaleService : ISaleService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateSaleRequest> _createValidator;
    private readonly IValidator<CreateSaleReturnRequest> _returnValidator;

    public SaleService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateSaleRequest> createValidator,
        IValidator<CreateSaleReturnRequest> returnValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _returnValidator = returnValidator;
    }

    public async Task<Result<SaleDto>> CreateAsync(
        Guid storeId,
        CreateSaleRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<SaleDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<SaleDto>.Fail(access.Errors, access.FailureCode);
        }

        if (_currentUser.UserId is null)
        {
            return Result<SaleDto>.Fail("Current user is required.", CatalogErrors.StoreAccessDenied);
        }

        var grouped = request.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (grouped.Count == 0)
        {
            return Result<SaleDto>.Fail("Cart is empty.", SalesErrors.EmptyCart);
        }

        var productIds = grouped.Select(g => g.ProductId).ToList();
        var products = await _db.Products
            .Where(p => p.StoreId == storeId && productIds.Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (products.Count != productIds.Count)
        {
            return Result<SaleDto>.Fail("One or more products were not found.", CatalogErrors.ProductNotFound);
        }

        var now = DateTime.UtcNow;
        // Active catalog discounts always apply at checkout (permission gates UI management only).
        var discountMap = await ResolveBestDiscountsAsync(storeId, products, now, cancellationToken)
            .ConfigureAwait(false);

        return await EfTransaction.ExecuteAsync(_db, async ct =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var nextInvoice = (await _db.Sales.AsNoTracking()
                .Where(s => s.StoreId == storeId)
                .Select(s => (int?)s.InvoiceNumber)
                .MaxAsync(ct)
                .ConfigureAwait(false) ?? 0) + 1;

            var sale = new Sale
            {
                StoreId = storeId,
                InvoiceNumber = nextInvoice,
                CashierUserId = _currentUser.UserId.Value,
                PaymentMethod = request.PaymentMethod,
                Status = SaleStatus.Completed,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            decimal subtotal = 0;
            decimal discountTotal = 0;
            var stockDeltas = new List<(Product Product, decimal Qty)>();

            foreach (var item in grouped)
            {
                var product = products.First(p => p.Id == item.ProductId);
                if (!product.IsActive)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return Result<SaleDto>.Fail($"Product '{product.NameEn}' is inactive.", SalesErrors.ProductInactive);
                }

                if (product.TracksInventory && product.StockQuantity < item.Quantity)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return Result<SaleDto>.Fail(
                        $"Insufficient stock for '{product.NameEn}'.",
                        SalesErrors.InsufficientStock);
                }

                var pct = discountMap.GetValueOrDefault(product.Id, 0m);
                var lineSub = Math.Round(product.Price * item.Quantity, 2, MidpointRounding.AwayFromZero);
                var lineDisc = Math.Round(lineSub * (pct / 100m), 2, MidpointRounding.AwayFromZero);
                var lineTotal = lineSub - lineDisc;

                sale.Lines.Add(new SaleLine
                {
                    ProductId = product.Id,
                    ProductNameAr = product.NameAr,
                    ProductNameEn = product.NameEn,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    DiscountPercent = pct,
                    LineSubtotal = lineSub,
                    LineTotal = lineTotal,
                    ReturnedQuantity = 0
                });

                subtotal += lineSub;
                discountTotal += lineDisc;
                if (product.TracksInventory)
                {
                    stockDeltas.Add((product, item.Quantity));
                }
            }

            sale.Subtotal = subtotal;
            sale.DiscountTotal = discountTotal;
            sale.GrandTotal = subtotal - discountTotal;

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            foreach (var (product, qty) in stockDeltas)
            {
                product.StockQuantity -= qty;
                _db.StockMovements.Add(new StockMovement
                {
                    StoreId = storeId,
                    ProductId = product.Id,
                    MovementType = StockMovementType.Sale,
                    QuantityChange = -qty,
                    QuantityAfter = product.StockQuantity,
                    ReferenceType = nameof(Sale),
                    ReferenceId = sale.Id,
                    Notes = $"Invoice #{nextInvoice}"
                });
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            var dto = await MapSaleDtoAsync(storeId, sale.Id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                return Result<SaleDto>.Fail("Sale was created but could not be loaded.", SalesErrors.SaleNotFound);
            }

            return Result<SaleDto>.Ok(dto);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<PagedResult<SaleListItemDto>>> GetPagedAsync(
        Guid storeId,
        SaleFilterRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<PagedResult<SaleListItemDto>>.Fail(access.Errors, access.FailureCode);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Sales.AsNoTracking().Where(s => s.StoreId == storeId);

        if (request.FromUtc is { } from)
        {
            query = query.Where(s => s.CreatedOnUtc >= from);
        }

        if (request.ToUtc is { } to)
        {
            query = query.Where(s => s.CreatedOnUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            if (int.TryParse(term, out var invoiceNo))
            {
                query = query.Where(s => s.InvoiceNumber == invoiceNo);
            }
            else
            {
                query = query.Where(s => s.Notes != null && s.Notes.Contains(term));
            }
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await (
                from s in query
                join u in _db.Users.AsNoTracking() on s.CashierUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                orderby s.CreatedOnUtc descending
                select new SaleListItemDto
                {
                    Id = s.Id,
                    InvoiceNumber = s.InvoiceNumber,
                    CashierUserId = s.CashierUserId,
                    CashierName = u != null ? (u.UserName ?? u.Email) : string.Empty,
                    PaymentMethod = s.PaymentMethod,
                    Status = s.Status,
                    Subtotal = s.Subtotal,
                    DiscountTotal = s.DiscountTotal,
                    GrandTotal = s.GrandTotal,
                    CreatedOnUtc = s.CreatedOnUtc,
                    LineCount = s.Lines.Count
                })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<SaleListItemDto>>.Ok(new PagedResult<SaleListItemDto>
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<SaleDto>> GetByIdAsync(
        Guid storeId,
        Guid saleId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<SaleDto>.Fail(access.Errors, access.FailureCode);
        }

        var dto = await MapSaleDtoAsync(storeId, saleId, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return Result<SaleDto>.Fail("The sale was not found.", SalesErrors.SaleNotFound);
        }

        return Result<SaleDto>.Ok(dto);
    }

    public async Task<Result<SaleReturnDto>> CreateReturnAsync(
        Guid storeId,
        Guid saleId,
        CreateSaleReturnRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _returnValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<SaleReturnDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<SaleReturnDto>.Fail(access.Errors, access.FailureCode);
        }

        if (_currentUser.UserId is null)
        {
            return Result<SaleReturnDto>.Fail("Current user is required.", CatalogErrors.StoreAccessDenied);
        }

        var sale = await _db.Sales
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (sale is null)
        {
            return Result<SaleReturnDto>.Fail("The sale was not found.", SalesErrors.SaleNotFound);
        }

        return await EfTransaction.ExecuteAsync(_db, async ct =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var nextReturn = (await _db.SaleReturns.AsNoTracking()
                .Where(r => r.StoreId == storeId)
                .Select(r => (int?)r.ReturnNumber)
                .MaxAsync(ct)
                .ConfigureAwait(false) ?? 0) + 1;

            var saleReturn = new SaleReturn
            {
                StoreId = storeId,
                SaleId = sale.Id,
                ReturnNumber = nextReturn,
                ProcessedByUserId = _currentUser.UserId.Value,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            decimal grand = 0;
            var productIds = sale.Lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Where(p => p.StoreId == storeId && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct)
                .ConfigureAwait(false);

            foreach (var item in request.Lines)
            {
                var line = sale.Lines.FirstOrDefault(l => l.Id == item.SaleLineId);
                if (line is null)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return Result<SaleReturnDto>.Fail("Invalid sale line.", SalesErrors.InvalidReturn);
                }

                var remaining = line.Quantity - line.ReturnedQuantity;
                if (item.Quantity <= 0 || item.Quantity > remaining)
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return Result<SaleReturnDto>.Fail(
                        $"Cannot return {item.Quantity} of '{line.ProductNameEn}'. Remaining: {remaining}.",
                        SalesErrors.InvalidReturn);
                }

                var unitNet = line.Quantity == 0
                    ? 0
                    : Math.Round(line.LineTotal / line.Quantity, 2, MidpointRounding.AwayFromZero);
                var lineTotal = Math.Round(unitNet * item.Quantity, 2, MidpointRounding.AwayFromZero);

                saleReturn.Lines.Add(new SaleReturnLine
                {
                    SaleLineId = line.Id,
                    ProductId = line.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = unitNet,
                    LineTotal = lineTotal
                });

                line.ReturnedQuantity += item.Quantity;
                grand += lineTotal;

                if (!products.TryGetValue(line.ProductId, out var product))
                {
                    await tx.RollbackAsync(ct).ConfigureAwait(false);
                    return Result<SaleReturnDto>.Fail("Product missing for return.", CatalogErrors.ProductNotFound);
                }

                if (product.TracksInventory)
                {
                    product.StockQuantity += item.Quantity;
                    _db.StockMovements.Add(new StockMovement
                    {
                        StoreId = storeId,
                        ProductId = product.Id,
                        MovementType = StockMovementType.Return,
                        QuantityChange = item.Quantity,
                        QuantityAfter = product.StockQuantity,
                        ReferenceType = nameof(SaleReturn),
                        Notes = $"Return #{nextReturn} / Invoice #{sale.InvoiceNumber}"
                    });
                }
            }

            saleReturn.GrandTotal = grand;

            var allReturned = sale.Lines.All(l => l.ReturnedQuantity >= l.Quantity);
            var anyReturned = sale.Lines.Any(l => l.ReturnedQuantity > 0);
            sale.Status = allReturned
                ? SaleStatus.Returned
                : anyReturned
                    ? SaleStatus.PartiallyReturned
                    : SaleStatus.Completed;

            _db.SaleReturns.Add(saleReturn);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            foreach (var movement in _db.ChangeTracker.Entries<StockMovement>()
                         .Where(e => e.State == EntityState.Added && e.Entity.ReferenceType == nameof(SaleReturn) && e.Entity.ReferenceId is null)
                         .Select(e => e.Entity))
            {
                movement.ReferenceId = saleReturn.Id;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            return Result<SaleReturnDto>.Ok(new SaleReturnDto
            {
                Id = saleReturn.Id,
                SaleId = sale.Id,
                ReturnNumber = saleReturn.ReturnNumber,
                GrandTotal = saleReturn.GrandTotal,
                Notes = saleReturn.Notes,
                CreatedOnUtc = saleReturn.CreatedOnUtc == default ? DateTime.UtcNow : saleReturn.CreatedOnUtc
            });
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, decimal>> ResolveBestDiscountsAsync(
        Guid storeId,
        IReadOnlyList<Product> products,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var discounts = await _db.ProductDiscounts.AsNoTracking()
            .Where(d => d.StoreId == storeId && d.IsActive)
            .Where(d => d.StartsAtUtc == null || d.StartsAtUtc <= nowUtc)
            .Where(d => d.EndsAtUtc == null || d.EndsAtUtc >= nowUtc)
            .Select(d => new
            {
                d.Id,
                d.DiscountPercent,
                d.AppliesToAllProducts,
                d.CategoryId,
                ProductIds = d.Items.Select(i => i.ProductId).ToList()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var map = new Dictionary<Guid, decimal>();
        foreach (var product in products)
        {
            decimal best = 0;
            foreach (var d in discounts)
            {
                var applies = d.AppliesToAllProducts
                    || (d.CategoryId is { } catId && product.CategoryId == catId)
                    || d.ProductIds.Contains(product.Id);
                if (applies && d.DiscountPercent > best)
                {
                    best = d.DiscountPercent;
                }
            }

            if (best > 0)
            {
                map[product.Id] = best;
            }
        }

        return map;
    }

    private async Task<SaleDto?> MapSaleDtoAsync(Guid storeId, Guid saleId, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales.AsNoTracking()
            .Where(s => s.Id == saleId && s.StoreId == storeId)
            .Select(s => new
            {
                s.Id,
                s.StoreId,
                s.InvoiceNumber,
                s.CashierUserId,
                s.PaymentMethod,
                s.Status,
                s.Subtotal,
                s.DiscountTotal,
                s.GrandTotal,
                s.Notes,
                s.CreatedOnUtc,
                Lines = s.Lines.Select(l => new SaleLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductNameAr = l.ProductNameAr,
                    ProductNameEn = l.ProductNameEn,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent,
                    LineSubtotal = l.LineSubtotal,
                    LineTotal = l.LineTotal,
                    ReturnedQuantity = l.ReturnedQuantity
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sale is null)
        {
            return null;
        }

        var cashier = await _db.Users.AsNoTracking()
            .Where(u => u.Id == sale.CashierUserId)
            .Select(u => u.UserName ?? u.Email)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SaleDto
        {
            Id = sale.Id,
            StoreId = sale.StoreId,
            InvoiceNumber = sale.InvoiceNumber,
            CashierUserId = sale.CashierUserId,
            CashierName = cashier ?? string.Empty,
            PaymentMethod = sale.PaymentMethod,
            Status = sale.Status,
            Subtotal = sale.Subtotal,
            DiscountTotal = sale.DiscountTotal,
            GrandTotal = sale.GrandTotal,
            Notes = sale.Notes,
            CreatedOnUtc = sale.CreatedOnUtc,
            Lines = sale.Lines
        };
    }
}
