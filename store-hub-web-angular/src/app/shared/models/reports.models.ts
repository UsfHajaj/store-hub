export interface ReportRangeParams {
  fromUtc?: string;
  toUtc?: string;
}

export interface NamedAmountDto {
  nameAr: string;
  nameEn: string;
  amount: number;
  count: number;
}

export interface DailySalesPointDto {
  /** ISO date (yyyy-MM-dd) */
  date: string;
  salesTotal: number;
  invoiceCount: number;
}

export interface HourlySalesPointDto {
  hour: number;
  salesTotal: number;
  invoiceCount: number;
}

export interface CashierPerformanceDto {
  userId: string | null;
  nameAr: string;
  nameEn: string;
  storeNameAr: string;
  storeNameEn: string;
  salesTotal: number;
  invoiceCount: number;
  averageTicket: number;
  returnsTotal: number;
  returnCount: number;
}

export interface InventoryStockItemDto {
  nameAr: string;
  nameEn: string;
  categoryNameAr: string;
  categoryNameEn: string;
  storeNameAr: string;
  storeNameEn: string;
  stockQuantity: number;
  reorderLevel: number;
  isLowStock: boolean;
}

export interface RecentOrderDto {
  invoiceNumber: number;
  storeNameAr: string;
  storeNameEn: string;
  cashierNameAr: string;
  cashierNameEn: string;
  grandTotal: number;
  paymentMethodAr: string;
  paymentMethodEn: string;
  createdOnUtc: string;
}

export interface StoreReportSummaryDto {
  storeId: string;
  storeNameAr: string;
  storeNameEn: string;
  salesTotal: number;
  discountTotal: number;
  returnsTotal: number;
  invoiceCount: number;
  returnCount: number;
  averageTicket: number;
  lowStockCount: number;
  productCount: number;
  dailySales: readonly DailySalesPointDto[];
  hourlySales: readonly HourlySalesPointDto[];
  topProducts: readonly NamedAmountDto[];
  topProductsByUnits: readonly NamedAmountDto[];
  salesByCategory: readonly NamedAmountDto[];
  salesByCashier: readonly NamedAmountDto[];
  cashiers: readonly CashierPerformanceDto[];
  paymentBreakdown: readonly NamedAmountDto[];
  lowStockProducts: readonly NamedAmountDto[];
  inventoryByCategory: readonly NamedAmountDto[];
  inventoryItems: readonly InventoryStockItemDto[];
  recentOrders: readonly RecentOrderDto[];
}

export interface AdminReportSummaryDto {
  salesTotal: number;
  discountTotal: number;
  returnsTotal: number;
  invoiceCount: number;
  returnCount: number;
  activeStoreCount: number;
  lowStockCount: number;
  productCount: number;
  salesByStore: readonly NamedAmountDto[];
  dailySales: readonly DailySalesPointDto[];
  hourlySales: readonly HourlySalesPointDto[];
  topProducts: readonly NamedAmountDto[];
  topProductsByUnits: readonly NamedAmountDto[];
  salesByCategory: readonly NamedAmountDto[];
  paymentBreakdown: readonly NamedAmountDto[];
  inventoryByCategory: readonly NamedAmountDto[];
  lowStockProducts: readonly NamedAmountDto[];
  cashiers: readonly CashierPerformanceDto[];
  inventoryItems: readonly InventoryStockItemDto[];
  recentOrders: readonly RecentOrderDto[];
  stores: readonly StoreReportSummaryDto[];
}
