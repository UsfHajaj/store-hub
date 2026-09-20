namespace StoreHub.Application.Common;

public static class SalesErrors
{
    public const string EmptyCart = "Sales.EmptyCart";

    public const string InsufficientStock = "Sales.InsufficientStock";

    public const string SaleNotFound = "Sales.SaleNotFound";

    public const string InvalidReturn = "Sales.InvalidReturn";

    public const string ProductInactive = "Sales.ProductInactive";
}

public static class InventoryErrors
{
    public const string ProductNotFound = "Inventory.ProductNotFound";

    public const string StocktakeNotFound = "Inventory.StocktakeNotFound";

    public const string StocktakeNotDraft = "Inventory.StocktakeNotDraft";

    public const string InvalidAdjustment = "Inventory.InvalidAdjustment";
}
