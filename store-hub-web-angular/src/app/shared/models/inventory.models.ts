export type StocktakeStatus = 1 | 2 | 3;

export const StocktakeStatuses = {
  Draft: 1 as StocktakeStatus,
  Completed: 2 as StocktakeStatus,
  Cancelled: 3 as StocktakeStatus,
} as const;

export interface InventoryItemDto {
  productId: string;
  nameAr: string;
  nameEn: string;
  barcode: string | null;
  categoryNameAr: string;
  categoryNameEn: string;
  stockQuantity: number;
  reorderLevel: number;
  isLowStock: boolean;
  isActive: boolean;
}

export interface InventoryFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  lowStockOnly?: boolean;
}

export interface AdjustStockPayload {
  productId: string;
  quantityChange: number;
  notes?: string | null;
}

export interface StocktakeListItemDto {
  id: string;
  status: StocktakeStatus;
  startedByUserId: string;
  startedByName: string;
  createdOnUtc: string;
  completedOnUtc: string | null;
  lineCount: number;
  notes: string | null;
}

export interface StocktakeLineDto {
  id: string;
  productId: string;
  productNameAr: string;
  productNameEn: string;
  systemQuantity: number;
  countedQuantity: number | null;
  difference: number;
}

export interface StocktakeDto {
  id: string;
  storeId: string;
  status: StocktakeStatus;
  startedByUserId: string;
  startedByName: string;
  createdOnUtc: string;
  completedOnUtc: string | null;
  notes: string | null;
  lines: readonly StocktakeLineDto[];
}

export interface CreateStocktakePayload {
  notes?: string | null;
}

export interface UpdateStocktakeLinePayload {
  productId: string;
  countedQuantity: number;
}

export interface UpdateStocktakeLinesPayload {
  lines: readonly UpdateStocktakeLinePayload[];
}

export const STOCKTAKE_STATUS_LABEL_KEYS: Record<StocktakeStatus, string> = {
  1: 'stocktake.statusDraft',
  2: 'stocktake.statusCompleted',
  3: 'stocktake.statusCancelled',
};
