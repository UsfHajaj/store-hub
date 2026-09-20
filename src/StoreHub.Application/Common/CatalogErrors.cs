namespace StoreHub.Application.Common;

public static class CatalogErrors
{
    public const string StoreAccessDenied = "Catalog.StoreAccessDenied";

    public const string CategoryNotFound = "Catalog.CategoryNotFound";

    public const string CategoryHasChildren = "Catalog.CategoryHasChildren";

    public const string CategoryHasProducts = "Catalog.CategoryHasProducts";

    public const string InvalidParentCategory = "Catalog.InvalidParentCategory";

    public const string ProductNotFound = "Catalog.ProductNotFound";

    public const string DuplicateBarcode = "Catalog.DuplicateBarcode";

    public const string DiscountNotFound = "Catalog.DiscountNotFound";

    public const string DiscountProductsRequired = "Catalog.DiscountProductsRequired";

    public const string InvalidDiscountPercent = "Catalog.InvalidDiscountPercent";
}
