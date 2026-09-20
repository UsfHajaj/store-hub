using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Stores.DTOs;

public sealed class StoreDto
{
    public Guid Id { get; init; }

    public string NameAr { get; init; } = string.Empty;

    public string NameEn { get; init; } = string.Empty;

    public string? DescriptionAr { get; init; }

    public string? DescriptionEn { get; init; }

    public StoreType StoreType { get; init; }

    public bool IsActive { get; init; }

    public Guid? OwnerUserId { get; init; }

    public string? InvoiceDisplayNameAr { get; init; }

    public string? InvoiceDisplayNameEn { get; init; }

    public string? LogoUrl { get; init; }

    public string? TaxNumber { get; init; }

    public string? InvoiceFooter { get; init; }

    public IReadOnlyList<Guid> MemberUserIds { get; init; } = Array.Empty<Guid>();
}

public sealed class UpdateInvoiceSettingsRequest
{
    public string? InvoiceDisplayNameAr { get; set; }

    public string? InvoiceDisplayNameEn { get; set; }

    public string? TaxNumber { get; set; }

    public string? InvoiceFooter { get; set; }

    public string? LogoUrl { get; set; }
}

public sealed class StoreLogoUploadDto
{
    public string ImageUrl { get; init; } = string.Empty;
}
