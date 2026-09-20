namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class SetRolePermissionsRequest
{
    public IReadOnlyList<Guid> PermissionIds { get; set; } = Array.Empty<Guid>();
}
