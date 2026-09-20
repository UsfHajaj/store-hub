using StoreHub.Application.Features.Catalog.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Catalog.Interfaces;

public interface ICategoryService
{
    Task<Result<PagedResult<CategoryDto>>> GetPagedAsync(
        Guid storeId,
        CategoryFilterRequest filter,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CategoryDto>>> GetListAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CategoryTreeNodeDto>>> GetTreeAsync(
        Guid storeId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> CreateAsync(
        Guid storeId,
        CreateCategoryRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> UpdateAsync(
        Guid storeId,
        Guid categoryId,
        UpdateCategoryRequest request,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid storeId,
        Guid categoryId,
        bool canManageAllStores,
        CancellationToken cancellationToken = default);
}
