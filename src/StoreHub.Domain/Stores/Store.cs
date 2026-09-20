using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Stores;

public sealed class Store : AuditableEntity
{
    public string NameAr { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public StoreHub.Domain.Enums.StoreType StoreType { get; set; } = StoreHub.Domain.Enums.StoreType.Other;

    public bool IsActive { get; set; } = true;

    public Guid? OwnerUserId { get; set; }

    public string? InvoiceDisplayNameAr { get; set; }

    public string? InvoiceDisplayNameEn { get; set; }

    public string? LogoUrl { get; set; }

    public string? TaxNumber { get; set; }

    public string? InvoiceFooter { get; set; }

    public ICollection<StoreMember> Members { get; set; } = new List<StoreMember>();
}
