using StoreHub.Application.Features.Catalog.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Catalog.Interfaces;

public interface IDiscountService
{
    Task<Result<PagedResult<DiscountDto>>> GetPagedAsync(
        Guid storeId,
        DiscountFilterRequest filter,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DiscountDto>>> GetAllAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<DiscountDto>> GetByIdAsync(
        Guid storeId,
        Guid discountId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<DiscountDto>> CreateAsync(
        Guid storeId,
        CreateDiscountRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<DiscountDto>> UpdateAsync(
        Guid storeId,
        Guid discountId,
        UpdateDiscountRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid storeId,
        Guid discountId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
