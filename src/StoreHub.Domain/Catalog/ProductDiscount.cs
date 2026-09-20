using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Catalog;

public sealed class ProductDiscount : AuditableEntity
{
    public Guid StoreId { get; set; }

    public string NameAr { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public decimal DiscountPercent { get; set; }

    public DateTime? StartsAtUtc { get; set; }

    public DateTime? EndsAtUtc { get; set; }

    public bool AppliesToAllProducts { get; set; }

    /// <summary>When set, discount applies to all products in this category (store-scoped).</summary>
    public Guid? CategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;

    public ProductCategory? Category { get; set; }

    public ICollection<ProductDiscountItem> Items { get; set; } = new List<ProductDiscountItem>();
}
