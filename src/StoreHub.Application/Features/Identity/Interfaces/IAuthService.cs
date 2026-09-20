using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Identity.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
