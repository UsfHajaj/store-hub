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

public sealed class ProductService : IProductService
{
    private readonly StoreHubDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly IValidator<ProductFilterRequest> _filterValidator;

    public ProductService(
        StoreHubDbContext db,
        ICurrentUserService currentUser,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        IValidator<ProductFilterRequest> filterValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<Result<PagedResult<ProductListItemDto>>> GetPagedAsync(
        Guid storeId,
        ProductFilterRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _filterValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<PagedResult<ProductListItemDto>>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<PagedResult<ProductListItemDto>>.Fail(access.Errors, access.FailureCode);
        }

        var page = request.Page <= 0 ? PaginationConstants.DefaultPage : request.Page;
        var pageSize = request.PageSize <= 0 ? PaginationConstants.DefaultPageSize : request.PageSize;
        if (pageSize > PaginationConstants.MaxPageSize)
        {
            pageSize = PaginationConstants.MaxPageSize;
        }

        var query = _db.Products.AsNoTracking().Where(p => p.StoreId == storeId);

        if (request.CategoryId is { } catId && catId != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == catId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.NameAr.Contains(term) ||
                p.NameEn.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)) ||
                (p.Sku != null && p.Sku.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderBy(p => p.NameEn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryNameAr = p.Category.NameAr,
                CategoryNameEn = p.Category.NameEn,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Barcode = p.Barcode,
                Sku = p.Sku,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                TracksInventory = p.TracksInventory,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<ProductListItemDto>>.Ok(new PagedResult<ProductListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<ProductDto>> GetByIdAsync(
        Guid storeId,
        Guid productId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<ProductDto>.Fail(access.Errors, access.FailureCode);
        }

        var dto = await MapProductDtoAsync(storeId, productId, cancellationToken).ConfigureAwait(false);
        if (dto is null)
        {
            return Result<ProductDto>.Fail("The product was not found.", CatalogErrors.ProductNotFound);
        }

        return Result<ProductDto>.Ok(dto);
    }

    public async Task<Result<ProductDto>> CreateAsync(
        Guid storeId,
        CreateProductRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ProductDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<ProductDto>.Fail(access.Errors, access.FailureCode);
        }

        var categoryOk = await _db.ProductCategories.AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId && c.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryOk)
        {
            return Result<ProductDto>.Fail("The category was not found.", CatalogErrors.CategoryNotFound);
        }

        var barcode = NormalizeOptional(request.Barcode);
        if (barcode is not null &&
            await BarcodeExistsAsync(storeId, barcode, null, cancellationToken).ConfigureAwait(false))
        {
            return Result<ProductDto>.Fail("A product with this barcode already exists in the store.", CatalogErrors.DuplicateBarcode);
        }

        var product = new Product
        {
            StoreId = storeId,
            CategoryId = request.CategoryId,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            DescriptionAr = NormalizeOptional(request.DescriptionAr),
            DescriptionEn = NormalizeOptional(request.DescriptionEn),
            Barcode = barcode,
            Sku = NormalizeOptional(request.Sku),
            Price = request.Price,
            ImageUrl = NormalizeOptional(request.ImageUrl),
            StockQuantity = request.TracksInventory ? request.StockQuantity : 0,
            TracksInventory = request.TracksInventory,
            IsActive = request.IsActive
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ProductDto>.Ok((await MapProductDtoAsync(storeId, product.Id, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result<ProductDto>> UpdateAsync(
        Guid storeId,
        Guid productId,
        UpdateProductRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<ProductDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return Result<ProductDto>.Fail(access.Errors, access.FailureCode);
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return Result<ProductDto>.Fail("The product was not found.", CatalogErrors.ProductNotFound);
        }

        var categoryOk = await _db.ProductCategories.AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId && c.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryOk)
        {
            return Result<ProductDto>.Fail("The category was not found.", CatalogErrors.CategoryNotFound);
        }

        var barcode = NormalizeOptional(request.Barcode);
        if (barcode is not null &&
            await BarcodeExistsAsync(storeId, barcode, productId, cancellationToken).ConfigureAwait(false))
        {
            return Result<ProductDto>.Fail("A product with this barcode already exists in the store.", CatalogErrors.DuplicateBarcode);
        }

        product.CategoryId = request.CategoryId;
        product.NameAr = request.NameAr.Trim();
        product.NameEn = request.NameEn.Trim();
        product.DescriptionAr = NormalizeOptional(request.DescriptionAr);
        product.DescriptionEn = NormalizeOptional(request.DescriptionEn);
        product.Barcode = barcode;
        product.Sku = NormalizeOptional(request.Sku);
        product.Price = request.Price;
        product.ImageUrl = NormalizeOptional(request.ImageUrl);
        product.TracksInventory = request.TracksInventory;
        product.StockQuantity = request.TracksInventory ? request.StockQuantity : 0;
        product.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ProductDto>.Ok((await MapProductDtoAsync(storeId, productId, cancellationToken).ConfigureAwait(false))!);
    }

    public async Task<Result> DeleteAsync(
        Guid storeId,
        Guid productId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default)
    {
        var access = await EnsureAccessAsync(storeId, canManageAllStores, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure)
        {
            return access;
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId, cancellationToken)
            .ConfigureAwait(false);
        if (product is null)
        {
            return Result.Fail("The product was not found.", CatalogErrors.ProductNotFound);
        }

        var discountLinks = await _db.ProductDiscountItems
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (discountLinks.Count > 0)
        {
            _db.ProductDiscountItems.RemoveRange(discountLinks);
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task<ProductDto?> MapProductDtoAsync(Guid storeId, Guid productId, CancellationToken cancellationToken) =>
        await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId && p.StoreId == storeId)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                StoreId = p.StoreId,
                CategoryId = p.CategoryId,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                DescriptionAr = p.DescriptionAr,
                DescriptionEn = p.DescriptionEn,
                Barcode = p.Barcode,
                Sku = p.Sku,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                TracksInventory = p.TracksInventory,
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<bool> BarcodeExistsAsync(
        Guid storeId,
        string barcode,
        Guid? excludeProductId,
        CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.StoreId == storeId && p.Barcode == barcode);
        if (excludeProductId is { } pid)
        {
            query = query.Where(p => p.Id != pid);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    private Task<Result> EnsureAccessAsync(Guid storeId, bool canManageAllStores, CancellationToken cancellationToken) =>
        StoreAccessHelper.EnsureStoreAccessAsync(_db, storeId, canManageAllStores, _currentUser.UserId, cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
