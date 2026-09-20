using StoreHub.Domain.Stores;
using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Catalog;

public sealed class ProductCategory : AuditableEntity
{
    public Guid StoreId { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public string NameAr { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public Store Store { get; set; } = null!;

    public ProductCategory? ParentCategory { get; set; }

    public ICollection<ProductCategory> ChildCategories { get; set; } = new List<ProductCategory>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
