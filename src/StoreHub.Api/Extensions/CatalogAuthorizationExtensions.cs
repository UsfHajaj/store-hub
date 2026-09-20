using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StoreHub.Application.Features.Identity;
using StoreHub.Shared.Api;
using StoreHub.Shared.Identity;

namespace StoreHub.Api.Extensions;

internal static class CatalogAuthorizationExtensions
{
    public static bool HasStoreManage(this ClaimsPrincipal user) =>
        user.HasClaim(StoreHubClaimTypes.Permission, PermissionCodes.StoreManage);

    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] codes) =>
        codes.Any(c => user.HasClaim(StoreHubClaimTypes.Permission, c));

    public static IActionResult? RequireAnyPermission(
        this ControllerBase controller,
        string? traceId,
        params string[] codes)
    {
        if (controller.User.HasAnyPermission(codes))
        {
            return null;
        }

        return controller.StatusCode(
            StatusCodes.Status403Forbidden,
            ApiResponse.FromFailure(new[] { "You do not have permission to perform this action." }, traceId));
    }
}
