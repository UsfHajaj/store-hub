using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Identity.Interfaces;

public interface IRoleService
{
    Task<Result<PagedResult<RoleListItemDto>>> GetPagedAsync(
        RoleFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RoleListItemDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<RoleDto>> SetPermissionsAsync(
        Guid id,
        SetRolePermissionsRequest request,
        CancellationToken cancellationToken = default);
}
