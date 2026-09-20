using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Inventory.DTOs;
using StoreHub.Application.Features.Inventory.Interfaces;
using StoreHub.Domain.Enums;
using StoreHub.Domain.Inventory;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Constants;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Inventory.Services;

public sealed class InventoryService : IInventoryService
{
    public const decimal DefaultReorderLevel = 5m;

    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<AdjustStockRequest> _adjustValidator;

    public InventoryService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        IValidator<AdjustStockRequest> adjustValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _adjustValidator = adjustValidator;
    }

    public async Task<Result<PagedResult<InventoryItemDto>>> GetPagedAsync(
        Guid storeId,
        InventoryFilterRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<PagedResult<InventoryItemDto>>.Fail(access.Errors, access.FailureCode);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query =
            from p in _db.Products.AsNoTracking()
            join c in _db.ProductCategories.AsNoTracking() on p.CategoryId equals c.Id
            where p.StoreId == storeId && p.TracksInventory
            select new { p, c };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.p.NameAr.Contains(term) ||
                x.p.NameEn.Contains(term) ||
                (x.p.Barcode != null && x.p.Barcode.Contains(term)));
        }

        if (request.LowStockOnly)
        {
            query = query.Where(x => x.p.StockQuantity <= (x.p.ReorderLevel ?? DefaultReorderLevel));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(x => x.p.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryItemDto
            {
                ProductId = x.p.Id,
                NameAr = x.p.NameAr,
                NameEn = x.p.NameEn,
                Barcode = x.p.Barcode,
                CategoryNameAr = x.c.NameAr,
                CategoryNameEn = x.c.NameEn,
                StockQuantity = x.p.StockQuantity,
                ReorderLevel = x.p.ReorderLevel ?? DefaultReorderLevel,
                IsLowStock = x.p.StockQuantity <= (x.p.ReorderLevel ?? DefaultReorderLevel),
                IsActive = x.p.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<InventoryItemDto>>.Ok(new PagedResult<InventoryItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<InventoryItemDto>> AdjustAsync(
        Guid storeId,
        AdjustStockRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _adjustValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<InventoryItemDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<InventoryItemDto>.Fail(access.Errors, access.FailureCode);
        }

        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return Result<InventoryItemDto>.Fail("Product not found.", InventoryErrors.ProductNotFound);
        }

        if (!product.TracksInventory)
        {
            return Result<InventoryItemDto>.Fail(
                "This product does not track inventory.",
                InventoryErrors.InvalidAdjustment);
        }

        var next = product.StockQuantity + request.QuantityChange;
        if (next < 0)
        {
            return Result<InventoryItemDto>.Fail("Stock cannot go below zero.", InventoryErrors.InvalidAdjustment);
        }

        product.StockQuantity = next;
        _db.StockMovements.Add(new StockMovement
        {
            StoreId = storeId,
            ProductId = product.Id,
            MovementType = StockMovementType.Adjustment,
            QuantityChange = request.QuantityChange,
            QuantityAfter = next,
            ReferenceType = "Adjustment",
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var reorder = product.ReorderLevel ?? DefaultReorderLevel;
        return Result<InventoryItemDto>.Ok(new InventoryItemDto
        {
            ProductId = product.Id,
            NameAr = product.NameAr,
            NameEn = product.NameEn,
            Barcode = product.Barcode,
            CategoryNameAr = product.Category.NameAr,
            CategoryNameEn = product.Category.NameEn,
            StockQuantity = product.StockQuantity,
            ReorderLevel = reorder,
            IsLowStock = product.StockQuantity <= reorder,
            IsActive = product.IsActive
        });
    }
}

public sealed class StocktakeService : IStocktakeService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public StocktakeService(StoreHubDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<StocktakeListItemDto>>> GetPagedAsync(
        Guid storeId,
        StocktakeFilterRequest filter,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<PagedResult<StocktakeListItemDto>>.Fail(access.Errors, access.FailureCode);
        }

        var page = filter.Page <= 0 ? PaginationConstants.DefaultPage : filter.Page;
        var pageSize = filter.PageSize <= 0 ? PaginationConstants.DefaultPageSize : filter.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query =
            from s in _db.Stocktakes.AsNoTracking()
            where s.StoreId == storeId
            join u in _db.Users.AsNoTracking() on s.StartedByUserId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new { s, u };

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var list = await query
            .OrderByDescending(x => x.s.CreatedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StocktakeListItemDto
            {
                Id = x.s.Id,
                Status = (byte)x.s.Status,
                StartedByUserId = x.s.StartedByUserId,
                StartedByName = x.u != null ? (x.u.UserName ?? x.u.Email) : string.Empty,
                CreatedOnUtc = x.s.CreatedOnUtc,
                CompletedOnUtc = x.s.CompletedOnUtc,
                LineCount = x.s.Lines.Count,
                Notes = x.s.Notes
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<StocktakeListItemDto>>.Ok(new PagedResult<StocktakeListItemDto>
        {
            Items = list,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<IReadOnlyList<StocktakeListItemDto>>> GetListAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<IReadOnlyList<StocktakeListItemDto>>.Fail(access.Errors, access.FailureCode);
        }

        var list = await (
                from s in _db.Stocktakes.AsNoTracking()
                where s.StoreId == storeId
                join u in _db.Users.AsNoTracking() on s.StartedByUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                orderby s.CreatedOnUtc descending
                select new StocktakeListItemDto
                {
                    Id = s.Id,
                    Status = (byte)s.Status,
                    StartedByUserId = s.StartedByUserId,
                    StartedByName = u != null ? (u.UserName ?? u.Email) : string.Empty,
                    CreatedOnUtc = s.CreatedOnUtc,
                    CompletedOnUtc = s.CompletedOnUtc,
                    LineCount = s.Lines.Count,
                    Notes = s.Notes
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<StocktakeListItemDto>>.Ok(list);
    }

    public async Task<Result<StocktakeDto>> GetByIdAsync(
        Guid storeId,
        Guid stocktakeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StocktakeDto>.Fail(access.Errors, access.FailureCode);
        }

        var dto = await MapAsync(storeId, stocktakeId, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return Result<StocktakeDto>.Fail("Stocktake not found.", InventoryErrors.StocktakeNotFound);
        }

        return Result<StocktakeDto>.Ok(dto);
    }

    public async Task<Result<StocktakeDto>> CreateAsync(
        Guid storeId,
        CreateStocktakeRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StocktakeDto>.Fail(access.Errors, access.FailureCode);
        }

        if (_currentUser.UserId is null)
        {
            return Result<StocktakeDto>.Fail("Current user is required.", CatalogErrors.StoreAccessDenied);
        }

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsActive)
            .Select(p => new { p.Id, p.StockQuantity })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stocktake = new Stocktake
        {
            StoreId = storeId,
            Status = StocktakeStatus.Draft,
            StartedByUserId = _currentUser.UserId.Value,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        foreach (var p in products)
        {
            stocktake.Lines.Add(new StocktakeLine
            {
                ProductId = p.Id,
                SystemQuantity = p.StockQuantity,
                CountedQuantity = null,
                Difference = 0
            });
        }

        _db.Stocktakes.Add(stocktake);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<StocktakeDto>.Ok((await MapAsync(storeId, stocktake.Id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result<StocktakeDto>> UpdateLinesAsync(
        Guid storeId,
        Guid stocktakeId,
        UpdateStocktakeLinesRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StocktakeDto>.Fail(access.Errors, access.FailureCode);
        }

        var stocktake = await _db.Stocktakes
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == stocktakeId && s.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (stocktake is null)
        {
            return Result<StocktakeDto>.Fail("Stocktake not found.", InventoryErrors.StocktakeNotFound);
        }

        if (stocktake.Status != StocktakeStatus.Draft)
        {
            return Result<StocktakeDto>.Fail("Only draft stocktakes can be edited.", InventoryErrors.StocktakeNotDraft);
        }

        foreach (var item in request.Lines)
        {
            var line = stocktake.Lines.FirstOrDefault(l => l.ProductId == item.ProductId);
            if (line is null)
            {
                continue;
            }

            line.CountedQuantity = item.CountedQuantity;
            line.Difference = item.CountedQuantity - line.SystemQuantity;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<StocktakeDto>.Ok((await MapAsync(storeId, stocktakeId, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result<StocktakeDto>> CompleteAsync(
        Guid storeId,
        Guid stocktakeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<StocktakeDto>.Fail(access.Errors, access.FailureCode);
        }

        var stocktake = await _db.Stocktakes
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == stocktakeId && s.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (stocktake is null)
        {
            return Result<StocktakeDto>.Fail("Stocktake not found.", InventoryErrors.StocktakeNotFound);
        }

        if (stocktake.Status != StocktakeStatus.Draft)
        {
            return Result<StocktakeDto>.Fail("Only draft stocktakes can be completed.", InventoryErrors.StocktakeNotDraft);
        }

        return await EfTransaction.ExecuteAsync(_db, async ct =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

            var productIds = stocktake.Lines.Select(l => l.ProductId).ToList();
            var products = await _db.Products
                .Where(p => p.StoreId == storeId && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct)
                .ConfigureAwait(false);

            foreach (var line in stocktake.Lines)
            {
                if (line.CountedQuantity is null)
                {
                    line.CountedQuantity = line.SystemQuantity;
                    line.Difference = 0;
                }

                if (line.Difference == 0 || !products.TryGetValue(line.ProductId, out var product))
                {
                    continue;
                }

                if (!product.TracksInventory)
                {
                    continue;
                }

                product.StockQuantity = line.CountedQuantity.Value;
                _db.StockMovements.Add(new StockMovement
                {
                    StoreId = storeId,
                    ProductId = product.Id,
                    MovementType = StockMovementType.Stocktake,
                    QuantityChange = line.Difference,
                    QuantityAfter = product.StockQuantity,
                    ReferenceType = nameof(Stocktake),
                    ReferenceId = stocktake.Id,
                    Notes = "Stocktake adjustment"
                });
            }

            stocktake.Status = StocktakeStatus.Completed;
            stocktake.CompletedOnUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            return Result<StocktakeDto>.Ok((await MapAsync(storeId, stocktakeId, ct).ConfigureAwait(false))!);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<StocktakeDto?> MapAsync(Guid storeId, Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.Stocktakes.AsNoTracking()
            .Where(s => s.Id == id && s.StoreId == storeId)
            .Select(s => new
            {
                s.Id,
                s.StoreId,
                s.Status,
                s.StartedByUserId,
                s.CreatedOnUtc,
                s.CompletedOnUtc,
                s.Notes,
                Lines = s.Lines.Select(l => new
                {
                    l.Id,
                    l.ProductId,
                    l.SystemQuantity,
                    l.CountedQuantity,
                    l.Difference,
                    l.Product.NameAr,
                    l.Product.NameEn
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var name = await _db.Users.AsNoTracking()
            .Where(u => u.Id == row.StartedByUserId)
            .Select(u => u.UserName ?? u.Email)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new StocktakeDto
        {
            Id = row.Id,
            StoreId = row.StoreId,
            Status = (byte)row.Status,
            StartedByUserId = row.StartedByUserId,
            StartedByName = name ?? string.Empty,
            CreatedOnUtc = row.CreatedOnUtc,
            CompletedOnUtc = row.CompletedOnUtc,
            Notes = row.Notes,
            Lines = row.Lines.Select(l => new StocktakeLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductNameAr = l.NameAr,
                ProductNameEn = l.NameEn,
                SystemQuantity = l.SystemQuantity,
                CountedQuantity = l.CountedQuantity,
                Difference = l.Difference
            }).ToList()
        };
    }
}
