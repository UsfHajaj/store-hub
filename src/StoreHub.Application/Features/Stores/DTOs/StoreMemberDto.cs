namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class StoreMemberDto
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string? NameAr { get; init; }

    public string? NameEn { get; init; }

    public string Email { get; init; } = string.Empty;
}
