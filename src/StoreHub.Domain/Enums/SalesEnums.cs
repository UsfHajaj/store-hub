namespace StoreHub.Domain.Enums;

public enum PaymentMethod : byte
{
    Cash = 1,
    Card = 2
}

public enum SaleStatus : byte
{
    Completed = 1,
    PartiallyReturned = 2,
    Returned = 3
}

public enum StockMovementType : byte
{
    Sale = 1,
    Return = 2,
    Adjustment = 3,
    Stocktake = 4,
    Receive = 5
}

public enum StocktakeStatus : byte
{
    Draft = 1,
    Completed = 2,
    Cancelled = 3
}
