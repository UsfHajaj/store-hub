namespace StoreHub.Domain.Enums;

public enum NotificationType : byte
{
    ApprovalAssigned = 1,

    ApprovalSubmitted = 2,

    ApprovalApproved = 3,

    ApprovalRejected = 4,

    LowStock = 10,

    SaleReturn = 11,

    StocktakeCompleted = 12,

    StockAdjusted = 13,

    StoreMembership = 14,

    Welcome = 15,

    General = 99
}
