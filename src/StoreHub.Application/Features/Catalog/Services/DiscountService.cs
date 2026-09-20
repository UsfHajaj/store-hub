using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Catalog.DTOs;
using StoreHub.Application.Features.Catalog.Interfaces;
using StoreHub.Domain.Catalog;
using StoreHub.Persistence;
using StoreHub.Shared.Api;
using StoreHub.Shared.Constants;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Catalog.Services;

public sealed class DiscountService : IDiscountService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateDiscountRequest> _createValidator;
    private readonly IValidator<UpdateDiscountRequest> _updateValidator;

    public DiscountService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateDiscountRequest> createValidator,
        IValidator<UpdateDiscountRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PagedResult<DiscountDto>>> GetPagedAsync(
        Guid storeId,
        DiscountFilterRequest filter,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<PagedResult<DiscountDto>>.Fail(access.Errors, access.FailureCode);
        }

        var page = filter.Page <= 0 ? PaginationConstants.DefaultPage : filter.Page;
        var pageSize = filter.PageSize <= 0 ? PaginationConstants.DefaultPageSize : filter.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var discounts = _db.ProductDiscounts.AsNoTracking().Where(d => d.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            discounts = discounts.Where(d => d.NameAr.Contains(term) || d.NameEn.Contains(term));
        }

        var total = await discounts.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await (
                from d in discounts
                join c in _db.ProductCategories.AsNoTracking() on d.CategoryId equals c.Id into catJoin
                from c in catJoin.DefaultIfEmpty()
                orderby d.CreatedOnUtc descending
                select new DiscountDto
                {
                    Id = d.Id,
                    StoreId = d.StoreId,
                    NameAr = d.NameAr,
                    NameEn = d.NameEn,
                    DiscountPercent = d.DiscountPercent,
                    StartsAtUtc = d.StartsAtUtc,
                    EndsAtUtc = d.EndsAtUtc,
                    AppliesToAllProducts = d.AppliesToAllProducts,
                    CategoryId = d.CategoryId,
                    CategoryNameAr = c != null ? c.NameAr : null,
                    CategoryNameEn = c != null ? c.NameEn : null,
                    IsActive = d.IsActive,
                    ProductCount = d.Items.Count,
                    ProductIds = d.Items.Select(i => i.ProductId).ToList()
                })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<DiscountDto>>.Ok(new PagedResult<DiscountDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<IReadOnlyList<DiscountDto>>> GetAllAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<IReadOnlyList<DiscountDto>>.Fail(access.Errors, access.FailureCode);
        }

        var discounts = _db.ProductDiscounts.AsNoTracking().Where(d => d.StoreId == storeId);
        var list = await (
                from d in discounts
                join c in _db.ProductCategories.AsNoTracking() on d.CategoryId equals c.Id into catJoin
                from c in catJoin.DefaultIfEmpty()
                orderby d.CreatedOnUtc descending
                select new DiscountDto
                {
                    Id = d.Id,
                    StoreId = d.StoreId,
                    NameAr = d.NameAr,
                    NameEn = d.NameEn,
                    DiscountPercent = d.DiscountPercent,
                    StartsAtUtc = d.StartsAtUtc,
                    EndsAtUtc = d.EndsAtUtc,
                    AppliesToAllProducts = d.AppliesToAllProducts,
                    CategoryId = d.CategoryId,
                    CategoryNameAr = c != null ? c.NameAr : null,
                    CategoryNameEn = c != null ? c.NameEn : null,
                    IsActive = d.IsActive,
                    ProductCount = d.Items.Count,
                    ProductIds = d.Items.Select(i => i.ProductId).ToList()
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<DiscountDto>>.Ok(list);
    }

    public async Task<Result<DiscountDto>> GetByIdAsync(
        Guid storeId,
        Guid discountId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<DiscountDto>.Fail(access.Errors, access.FailureCode);
        }

        var dto = await MapDiscountDtoAsync(storeId, discountId, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return Result<DiscountDto>.Fail("The discount was not found.", CatalogErrors.DiscountNotFound);
        }

        return Result<DiscountDto>.Ok(dto);
    }

    public async Task<Result<DiscountDto>> CreateAsync(
        Guid storeId,
        CreateDiscountRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<DiscountDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<DiscountDto>.Fail(access.Errors, access.FailureCode);
        }

        var scope = await ResolveScopeAsync(storeId, request.AppliesToAllProducts, request.CategoryId, request.ProductIds, cancellationToken)
            .ConfigureAwait(false);
        if (scope.IsFailure)
        {
            return Result<DiscountDto>.Fail(scope.Errors, scope.FailureCode);
        }

        var discount = new ProductDiscount
        {
            StoreId = storeId,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            DiscountPercent = request.DiscountPercent,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            AppliesToAllProducts = scope.Value!.AppliesToAll,
            CategoryId = scope.Value.CategoryId,
            IsActive = request.IsActive
        };

        _db.ProductDiscounts.Add(discount);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var pid in scope.Value.ProductIds)
        {
            _db.ProductDiscountItems.Add(new ProductDiscountItem { DiscountId = discount.Id, ProductId = pid });
        }

        if (scope.Value.ProductIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<DiscountDto>.Ok((await MapDiscountDtoAsync(storeId, discount.Id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result<DiscountDto>> UpdateAsync(
        Guid storeId,
        Guid discountId,
        UpdateDiscountRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<DiscountDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<DiscountDto>.Fail(access.Errors, access.FailureCode);
        }

        var discount = await _db.ProductDiscounts
            .FirstOrDefaultAsync(d => d.Id == discountId && d.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (discount is null)
        {
            return Result<DiscountDto>.Fail("The discount was not found.", CatalogErrors.DiscountNotFound);
        }

        var scope = await ResolveScopeAsync(storeId, request.AppliesToAllProducts, request.CategoryId, request.ProductIds, cancellationToken)
            .ConfigureAwait(false);
        if (scope.IsFailure)
        {
            return Result<DiscountDto>.Fail(scope.Errors, scope.FailureCode);
        }

        discount.NameAr = request.NameAr.Trim();
        discount.NameEn = request.NameEn.Trim();
        discount.DiscountPercent = request.DiscountPercent;
        discount.StartsAtUtc = request.StartsAtUtc;
        discount.EndsAtUtc = request.EndsAtUtc;
        discount.AppliesToAllProducts = scope.Value!.AppliesToAll;
        discount.CategoryId = scope.Value.CategoryId;
        discount.IsActive = request.IsActive;

        var existingItems = await _db.ProductDiscountItems
            .Where(i => i.DiscountId == discountId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.ProductDiscountItems.RemoveRange(existingItems);

        foreach (var pid in scope.Value.ProductIds)
        {
            _db.ProductDiscountItems.Add(new ProductDiscountItem { DiscountId = discountId, ProductId = pid });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DiscountDto>.Ok((await MapDiscountDtoAsync(storeId, discountId, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result> DeleteAsync(
        Guid storeId,
        Guid discountId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access;
        }

        var discount = await _db.ProductDiscounts
            .FirstOrDefaultAsync(d => d.Id == discountId && d.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (discount is null)
        {
            return Result.Fail("The discount was not found.", CatalogErrors.DiscountNotFound);
        }

        _db.ProductDiscounts.Remove(discount);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private sealed record DiscountScope(bool AppliesToAll, Guid? CategoryId, IReadOnlyList<Guid> ProductIds);

    private async Task<Result<DiscountScope>> ResolveScopeAsync(
        Guid storeId,
        bool appliesToAll,
        Guid? categoryId,
        IReadOnlyList<Guid>? productIds,
        CancellationToken cancellationToken)
    {
        if (appliesToAll)
        {
            return Result<DiscountScope>.Ok(new DiscountScope(true, null, Array.Empty<Guid>()));
        }

        if (categoryId is { } catId && catId != Guid.Empty)
        {
            var exists = await _db.ProductCategories.AsNoTracking()
                .AnyAsync(c => c.Id == catId && c.StoreId == storeId, cancellationToken)
                .ConfigureAwait(false);
            if (!exists)
            {
                return Result<DiscountScope>.Fail("The category was not found.", CatalogErrors.CategoryNotFound);
            }

            return Result<DiscountScope>.Ok(new DiscountScope(false, catId, Array.Empty<Guid>()));
        }

        var ids = productIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            return Result<DiscountScope>.Fail(
                "Choose all products, a category, or at least one product.",
                CatalogErrors.DiscountProductsRequired);
        }

        var found = await _db.Products.AsNoTracking()
            .Where(p => p.StoreId == storeId && ids.Contains(p.Id))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (found != ids.Count)
        {
            return Result<DiscountScope>.Fail("One or more products were not found in this store.", CatalogErrors.ProductNotFound);
        }

        return Result<DiscountScope>.Ok(new DiscountScope(false, null, ids));
    }

    private async Task<DiscountDto?> MapDiscountDtoAsync(Guid storeId, Guid discountId, CancellationToken cancellationToken)
    {
        return await (
                from d in _db.ProductDiscounts.AsNoTracking()
                where d.Id == discountId && d.StoreId == storeId
                join c in _db.ProductCategories.AsNoTracking() on d.CategoryId equals c.Id into catJoin
                from c in catJoin.DefaultIfEmpty()
                select new DiscountDto
                {
                    Id = d.Id,
                    StoreId = d.StoreId,
                    NameAr = d.NameAr,
                    NameEn = d.NameEn,
                    DiscountPercent = d.DiscountPercent,
                    StartsAtUtc = d.StartsAtUtc,
                    EndsAtUtc = d.EndsAtUtc,
                    AppliesToAllProducts = d.AppliesToAllProducts,
                    CategoryId = d.CategoryId,
                    CategoryNameAr = c != null ? c.NameAr : null,
                    CategoryNameEn = c != null ? c.NameEn : null,
                    IsActive = d.IsActive,
                    ProductCount = d.Items.Count,
                    ProductIds = d.Items.Select(i => i.ProductId).ToList()
                })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<Result> EnsureAccessAsync(Guid storeId, bool canManageAllStores, CancellationToken cancellationToken) =>
        StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken);
}
