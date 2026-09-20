namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class AssignUserRolesRequest
{
    public IReadOnlyList<Guid> RoleIds { get; set; } = Array.Empty<Guid>();
}
