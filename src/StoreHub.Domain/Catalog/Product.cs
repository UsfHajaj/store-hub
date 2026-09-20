using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Catalog;

public sealed class Product : AuditableEntity
{
    public Guid StoreId { get; set; }

    public Guid CategoryId { get; set; }

    public string NameAr { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }

    public string? Barcode { get; set; }

    public string? Sku { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public decimal StockQuantity { get; set; }

    /// <summary>
    /// When false (e.g. coffee / made-to-order), stock is not checked or decremented on sale.
    /// </summary>
    public bool TracksInventory { get; set; } = true;

    public decimal? ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;

    public ProductCategory Category { get; set; } = null!;

    public ICollection<ProductDiscountItem> DiscountItems { get; set; } = new List<ProductDiscountItem>();
}
