namespace StoreHub.Application.Features.Identity;

/// <summary>Normalized permission codes for policies and seeding.</summary>
public static class PermissionCodes
{
    public const string UserManage = "USER_MANAGE";

    public const string RoleManage = "ROLE_MANAGE";

    public const string NotificationView = "NOTIFICATION_VIEW";

    public const string NotificationManage = "NOTIFICATION_MANAGE";

    public const string AuditLogView = "AUDIT_LOG_VIEW";

    public const string StoreManage = "STORE_MANAGE";

    public const string StoreView = "STORE_VIEW";

    public const string PosSaleCreate = "POS_SALE_CREATE";

    public const string PosSaleView = "POS_SALE_VIEW";

    public const string PosProductCreate = "POS_PRODUCT_CREATE";

    public const string PosProductUpdate = "POS_PRODUCT_UPDATE";

    public const string PosCatalogView = "POS_CATALOG_VIEW";

    public const string PosDiscountApply = "POS_DISCOUNT_APPLY";

    public const string PosDiscountManage = "POS_DISCOUNT_MANAGE";

    public const string PosCategoryManage = "POS_CATEGORY_MANAGE";

    public const string PosInventoryManage = "POS_INVENTORY_MANAGE";

    public const string PosStocktakeManage = "POS_STOCKTAKE_MANAGE";

    public const string PosReturnCreate = "POS_RETURN_CREATE";

    public const string ReportView = "REPORT_VIEW";
}
