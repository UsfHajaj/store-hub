using StoreHub.Application.Features.Inventory.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Inventory.Interfaces;

public interface IInventoryService
{
    Task<Result<PagedResult<InventoryItemDto>>> GetPagedAsync(
        Guid storeId,
        InventoryFilterRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryItemDto>> AdjustAsync(
        Guid storeId,
        AdjustStockRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}

public interface IStocktakeService
{
    Task<Result<PagedResult<StocktakeListItemDto>>> GetPagedAsync(
        Guid storeId,
        StocktakeFilterRequest filter,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StocktakeListItemDto>>> GetListAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<StocktakeDto>> GetByIdAsync(
        Guid storeId,
        Guid stocktakeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<StocktakeDto>> CreateAsync(
        Guid storeId,
        CreateStocktakeRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<StocktakeDto>> UpdateLinesAsync(
        Guid storeId,
        Guid stocktakeId,
        UpdateStocktakeLinesRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<StocktakeDto>> CompleteAsync(
        Guid storeId,
        Guid stocktakeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
