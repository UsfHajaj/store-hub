using StoreHub.Application.Features.Catalog.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Catalog.Interfaces;

public interface IProductService
{
    Task<Result<PagedResult<ProductListItemDto>>> GetPagedAsync(
        Guid storeId,
        ProductFilterRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDto>> GetByIdAsync(
        Guid storeId,
        Guid productId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDto>> CreateAsync(
        Guid storeId,
        CreateProductRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDto>> UpdateAsync(
        Guid storeId,
        Guid productId,
        UpdateProductRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid storeId,
        Guid productId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
