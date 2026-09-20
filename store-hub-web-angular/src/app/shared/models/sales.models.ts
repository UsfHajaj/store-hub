export type PaymentMethod = 1 | 2;
export type SaleStatus = 1 | 2 | 3;

export const PaymentMethods = {
  Cash: 1 as PaymentMethod,
  Card: 2 as PaymentMethod,
} as const;

export const SaleStatuses = {
  Completed: 1 as SaleStatus,
  PartiallyReturned: 2 as SaleStatus,
  Returned: 3 as SaleStatus,
} as const;

export interface SaleListItemDto {
  id: string;
  invoiceNumber: number;
  cashierUserId: string;
  cashierName: string;
  paymentMethod: PaymentMethod;
  status: SaleStatus;
  subtotal: number;
  discountTotal: number;
  grandTotal: number;
  createdOnUtc: string;
  lineCount: number;
}

export interface SaleLineDto {
  id: string;
  productId: string;
  productNameAr: string;
  productNameEn: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  lineSubtotal: number;
  lineTotal: number;
  returnedQuantity: number;
}

export interface SaleDto {
  id: string;
  storeId: string;
  invoiceNumber: number;
  cashierUserId: string;
  cashierName: string;
  paymentMethod: PaymentMethod;
  status: SaleStatus;
  subtotal: number;
  discountTotal: number;
  grandTotal: number;
  notes: string | null;
  createdOnUtc: string;
  lines: readonly SaleLineDto[];
}

export interface SaleFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  fromUtc?: string;
  toUtc?: string;
}

export interface CreateSaleLinePayload {
  productId: string;
  quantity: number;
}

export interface CreateSalePayload {
  paymentMethod: PaymentMethod;
  notes?: string | null;
  lines: readonly CreateSaleLinePayload[];
}

export interface CreateSaleReturnLinePayload {
  saleLineId: string;
  quantity: number;
}

export interface CreateSaleReturnPayload {
  notes?: string | null;
  lines: readonly CreateSaleReturnLinePayload[];
}

export interface SaleReturnDto {
  id: string;
  saleId: string;
  returnNumber: number;
  grandTotal: number;
  notes: string | null;
  createdOnUtc: string;
}

export const PAYMENT_METHOD_OPTIONS: { value: PaymentMethod; labelKey: string }[] = [
  { value: 1, labelKey: 'pos.payCash' },
  { value: 2, labelKey: 'pos.payCard' },
];

export const PAYMENT_METHOD_LABEL_KEYS: Record<PaymentMethod, string> = {
  1: 'pos.payCash',
  2: 'pos.payCard',
};

export const SALE_STATUS_LABEL_KEYS: Record<SaleStatus, string> = {
  1: 'orders.statusCompleted',
  2: 'orders.statusPartiallyReturned',
  3: 'orders.statusReturned',
};
