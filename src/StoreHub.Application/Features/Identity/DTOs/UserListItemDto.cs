namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class UserListItemDto
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string? NameAr { get; init; }

    public string? NameEn { get; init; }

    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTime? LastLoginUtc { get; init; }
}
