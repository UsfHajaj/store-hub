using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class StoreListItemDto
{
    public Guid Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public StoreType StoreType { get; init; }

    public bool IsActive { get; init; }

    public int MemberCount { get; init; }
}
