using StoreHub.Application.Features.Stores.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Stores.Interfaces;

public interface IStoreService
{
    Task<Result<PagedResult<StoreListItemDto>>> GetPagedAsync(
        StoreFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoreListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoreListItemDto>>> GetMineAsync(
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<StoreDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<StoreDto>> GetByIdForMemberAsync(
        Guid id,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<StoreDto>> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);

    Task<Result<StoreDto>> UpdateAsync(Guid id, UpdateStoreRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoreMemberDto>>> GetMembersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<StoreMemberDto>>> SetMembersAsync(
        Guid id,
        SetStoreMembersRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<StoreDto>> UpdateInvoiceSettingsAsync(
        Guid id,
        UpdateInvoiceSettingsRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
