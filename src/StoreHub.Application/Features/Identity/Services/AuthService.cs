using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreHub.Application.Common;
using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Persistence;
using StoreHub.Shared.Results;

namespace StoreHub.Application.Features.Identity.Services;

public sealed class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly StoreHubDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenIssuer _jwtTokenIssuer;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        ILogger<AuthService> logger,
        StoreHubDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenIssuer jwtTokenIssuer,
        IValidator<LoginRequest> loginValidator)
    {
        _logger = logger;
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenIssuer = jwtTokenIssuer;
        _loginValidator = loginValidator;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<LoginResponseDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid credentials or unknown account.");
            return Result<LoginResponseDto>.Fail("Invalid email or password.", IdentityErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Fail("This account is inactive.", IdentityErrors.UserInactive);
        }

        var permissionCodes = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        var issued = _jwtTokenIssuer.IssueAccessToken(user.Id, user.UserName, user.Email, permissionCodes);

        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<LoginResponseDto>.Ok(new LoginResponseDto
        {
            AccessToken = issued.Token,
            ExpiresAtUtc = issued.ExpiresAtUtc,
            TokenType = "Bearer"
        });
    }
}
