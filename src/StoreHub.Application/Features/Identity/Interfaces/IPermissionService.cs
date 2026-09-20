using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Identity.Interfaces;

public interface IPermissionService
{
    Task<Result> SeedDefaultPermissionsAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}
