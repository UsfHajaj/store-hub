using Microsoft.AspNetCore.Authorization;
using StoreHub.Shared.Identity;

namespace StoreHub.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var ok = context.User.Claims.Any(c =>
            c.Type == StoreHubClaimTypes.Permission &&
            string.Equals(c.Value, requirement.PermissionCode, StringComparison.Ordinal));

        if (ok)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
