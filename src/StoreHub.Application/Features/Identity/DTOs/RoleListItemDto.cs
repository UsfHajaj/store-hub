namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class RoleListItemDto
{
    public Guid Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public bool IsSystemRole { get; init; }

    public int PermissionCount { get; init; }

    public int UserCount { get; init; }
}
