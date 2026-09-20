using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Identity.Interfaces;

public interface IUserService
{
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> AssignRolesAsync(Guid id, AssignUserRolesRequest request, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<UserListItemDto>>> GetPagedAsync(UserFilterRequest request, CancellationToken cancellationToken = default);
}
