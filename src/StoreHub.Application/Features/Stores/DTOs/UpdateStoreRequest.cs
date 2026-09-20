using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class UpdateStoreRequest
{
    public string NameAr { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public StoreType StoreType { get; set; } = StoreType.Other;
}
