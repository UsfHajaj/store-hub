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

public sealed class CategoryService : ICategoryService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;

    public CategoryService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PagedResult<CategoryDto>>> GetPagedAsync(
        Guid storeId,
        CategoryFilterRequest filter,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<PagedResult<CategoryDto>>.Fail(access.Errors, access.FailureCode);
        }

        var page = filter.Page <= 0 ? PaginationConstants.DefaultPage : filter.Page;
        var pageSize = filter.PageSize <= 0 ? PaginationConstants.DefaultPageSize : filter.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = QueryCategories(storeId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(c => c.NameAr.Contains(term) || c.NameEn.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<CategoryDto>>.Ok(new PagedResult<CategoryDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetListAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<IReadOnlyList<CategoryDto>>.Fail(access.Errors, access.FailureCode);
        }

        var list = await QueryCategories(storeId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.NameEn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<CategoryDto>>.Ok(list);
    }

    public async Task<Result<IReadOnlyList<CategoryTreeNodeDto>>> GetTreeAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<IReadOnlyList<CategoryTreeNodeDto>>.Fail(access.Errors, access.FailureCode);
        }

        var flat = await QueryCategories(storeId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.NameEn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var nodes = flat.ToDictionary(
            c => c.Id,
            c => new CategoryTreeNodeDto
            {
                Id = c.Id,
                ParentCategoryId = c.ParentCategoryId,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ProductCount = c.ProductCount,
                Children = new List<CategoryTreeNodeDto>()
            });

        var roots = new List<CategoryTreeNodeDto>();
        foreach (var c in flat)
        {
            var node = nodes[c.Id];
            if (c.ParentCategoryId is { } parentId && nodes.TryGetValue(parentId, out var parent))
            {
                ((List<CategoryTreeNodeDto>)parent.Children).Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return Result<IReadOnlyList<CategoryTreeNodeDto>>.Ok(roots);
    }

    public async Task<Result<CategoryDto>> CreateAsync(
        Guid storeId,
        CreateCategoryRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<CategoryDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<CategoryDto>.Fail(access.Errors, access.FailureCode);
        }

        var parentCheck = await ValidateParentAsync(storeId, request.ParentCategoryId, null, cancellationToken)
            .ConfigureAwait(false);
        if (parentCheck.IsFailure)
        {
            return Result<CategoryDto>.Fail(parentCheck.Errors, parentCheck.FailureCode);
        }

        var sortOrder = await NextSortOrderAsync(storeId, request.ParentCategoryId, cancellationToken)
            .ConfigureAwait(false);

        var entity = new ProductCategory
        {
            StoreId = storeId,
            ParentCategoryId = request.ParentCategoryId,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            SortOrder = sortOrder,
            IsActive = request.IsActive
        };

        _db.ProductCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<CategoryDto>.Ok(await MapCategoryDtoAsync(entity.Id, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(
        Guid storeId,
        Guid categoryId,
        UpdateCategoryRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<CategoryDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<CategoryDto>.Fail(access.Errors, access.FailureCode);
        }

        var category = await _db.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return Result<CategoryDto>.Fail("The category was not found.", CatalogErrors.CategoryNotFound);
        }

        if (request.ParentCategoryId == categoryId)
        {
            return Result<CategoryDto>.Fail("A category cannot be its own parent.", CatalogErrors.InvalidParentCategory);
        }

        var parentCheck = await ValidateParentAsync(storeId, request.ParentCategoryId, categoryId, cancellationToken)
            .ConfigureAwait(false);
        if (parentCheck.IsFailure)
        {
            return Result<CategoryDto>.Fail(parentCheck.Errors, parentCheck.FailureCode);
        }

        if (category.ParentCategoryId != request.ParentCategoryId)
        {
            category.SortOrder = await NextSortOrderAsync(storeId, request.ParentCategoryId, cancellationToken)
                .ConfigureAwait(false);
        }

        category.ParentCategoryId = request.ParentCategoryId;
        category.NameAr = request.NameAr.Trim();
        category.NameEn = request.NameEn.Trim();
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<CategoryDto>.Ok(await MapCategoryDtoAsync(categoryId, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> DeleteAsync(
        Guid storeId,
        Guid categoryId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access;
        }

        var category = await _db.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            return Result.Fail("The category was not found.", CatalogErrors.CategoryNotFound);
        }

        if (await _db.ProductCategories.AsNoTracking()
                .AnyAsync(c => c.ParentCategoryId == categoryId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Fail("Cannot delete a category that has subcategories.", CatalogErrors.CategoryHasChildren);
        }

        if (await _db.Products.AsNoTracking()
                .AnyAsync(p => p.CategoryId == categoryId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Fail("Cannot delete a category that has products.", CatalogErrors.CategoryHasProducts);
        }

        _db.ProductCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task<int> NextSortOrderAsync(
        Guid storeId,
        Guid? parentCategoryId,
        CancellationToken cancellationToken)
    {
        var siblings = _db.ProductCategories.AsNoTracking()
            .Where(c => c.StoreId == storeId && c.ParentCategoryId == parentCategoryId);

        var max = await siblings.Select(c => (int?)c.SortOrder).MaxAsync(cancellationToken).ConfigureAwait(false);
        return (max ?? -1) + 1;
    }

    private IQueryable<CategoryDto> QueryCategories(Guid storeId) =>
        _db.ProductCategories.AsNoTracking()
            .Where(c => c.StoreId == storeId)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                StoreId = c.StoreId,
                ParentCategoryId = c.ParentCategoryId,
                ParentNameAr = c.ParentCategory != null ? c.ParentCategory.NameAr : null,
                ParentNameEn = c.ParentCategory != null ? c.ParentCategory.NameEn : null,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count,
                ChildCount = c.ChildCategories.Count
            });

    private async Task<CategoryDto> MapCategoryDtoAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.ProductCategories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                StoreId = c.StoreId,
                ParentCategoryId = c.ParentCategoryId,
                ParentNameAr = c.ParentCategory != null ? c.ParentCategory.NameAr : null,
                ParentNameEn = c.ParentCategory != null ? c.ParentCategory.NameEn : null,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count,
                ChildCount = c.ChildCategories.Count
            })
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<Result> ValidateParentAsync(
        Guid storeId,
        Guid? parentId,
        Guid? editingCategoryId,
        CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return Result.Ok();
        }

        var parent = await _db.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == parentId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (parent is null || parent.StoreId != storeId)
        {
            return Result.Fail("The parent category is invalid for this store.", CatalogErrors.InvalidParentCategory);
        }

        if (editingCategoryId is null)
        {
            return Result.Ok();
        }

        var currentId = editingCategoryId.Value;
        var walk = parentId;
        while (walk is { } pid)
        {
            if (pid == currentId)
            {
                return Result.Fail("Cannot assign a descendant as parent.", CatalogErrors.InvalidParentCategory);
            }

            walk = await _db.ProductCategories.AsNoTracking()
                .Where(c => c.Id == pid)
                .Select(c => c.ParentCategoryId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return Result.Ok();
    }

    private Task<Result> EnsureAccessAsync(Guid storeId, bool canManageAllStores, CancellationToken cancellationToken) =>
        StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken);
}
