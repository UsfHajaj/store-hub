namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class RoleDto
{
    public Guid Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public bool IsSystemRole { get; init; }

    public IReadOnlyList<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}
