using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class CreateStoreRequest
{
    public string NameAr { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public StoreType StoreType { get; set; } = StoreType.Other;

    public IReadOnlyList<Guid> MemberUserIds { get; set; } = Array.Empty<Guid>();
}
