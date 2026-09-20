namespace StoreHub.Application.Features.Identity;

internal static class PermissionDefinitions
{
    internal sealed record Definition(
        string Code,
        string NameAr,
        string NameEn,
        string? DescriptionAr,
        string? DescriptionEn,
        string Module);

    internal static readonly IReadOnlyList<Definition> All =
    [
        new(PermissionCodes.UserManage, "إدارة المستخدمين", "Manage users", null, "Manage users", "Identity"),
        new(PermissionCodes.RoleManage, "إدارة الأدوار والصلاحيات", "Manage roles and permissions", null, "Manage roles and permissions", "Identity"),
        new(PermissionCodes.NotificationView, "عرض قوالب وسجلات الإشعارات", "View notification templates and records", null, "View notifications", "Notifications"),
        new(PermissionCodes.NotificationManage, "إنشاء الإشعارات وإدارة القوالب", "Create notifications and manage templates", null, "Manage notifications", "Notifications"),
        new(PermissionCodes.AuditLogView, "عرض سجل العمليات", "View audit log", null, "View audit log", "Audit"),
        new(PermissionCodes.StoreManage, "إدارة المحلات والأعضاء", "Manage stores and members", null, "Manage stores and members", "Stores"),
        new(PermissionCodes.StoreView, "عرض المحلات المعينة", "View assigned stores", null, "View assigned stores", "Stores"),
        new(PermissionCodes.PosSaleCreate, "إجراء عملية بيع / محاسبة", "Create a sale / checkout", "يسمح للكاشير بإنهاء البيع فقط", "Allows cashier checkout only", "POS"),
        new(PermissionCodes.PosSaleView, "عرض المبيعات والفواتير", "View sales and invoices", null, "Browse orders and reprint invoices", "POS"),
        new(PermissionCodes.PosProductCreate, "إضافة منتج", "Create product", null, "Add products", "POS"),
        new(PermissionCodes.PosProductUpdate, "تعديل منتج", "Update product", null, "Edit products", "POS"),
        new(PermissionCodes.PosCatalogView, "عرض فهرس المحل", "View store catalog", null, "Browse categories and products", "POS"),
        new(PermissionCodes.PosCategoryManage, "إدارة التصنيفات", "Manage categories", null, "Create and edit product categories", "POS"),
        new(PermissionCodes.PosDiscountManage, "إدارة الخصومات", "Manage discounts", null, "Configure product discounts", "POS"),
        new(PermissionCodes.PosDiscountApply, "تطبيق خصم", "Apply discount", null, "Apply discounts at checkout", "POS"),
        new(PermissionCodes.PosInventoryManage, "إدارة المخزون", "Manage inventory", null, "View stock and adjust quantities", "POS"),
        new(PermissionCodes.PosStocktakeManage, "إدارة الجرد", "Manage stocktaking", null, "Create and complete stocktakes", "POS"),
        new(PermissionCodes.PosReturnCreate, "إنشاء مرتجع", "Create return", null, "Process sale returns", "POS"),
        new(PermissionCodes.ReportView, "عرض التقارير", "View reports", null, "View sales and inventory reports", "Reports")
    ];
}
