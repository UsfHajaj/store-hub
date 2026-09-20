export interface CategoryDto {
  id: string;
  storeId: string;
  parentCategoryId: string | null;
  parentNameAr?: string | null;
  parentNameEn?: string | null;
  nameAr: string;
  nameEn: string;
  sortOrder: number;
  isActive: boolean;
  productCount: number;
  childCount: number;
}

export interface CategoryFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface CategoryTreeNodeDto {
  id: string;
  parentCategoryId: string | null;
  nameAr: string;
  nameEn: string;
  sortOrder: number;
  isActive: boolean;
  productCount: number;
  children: readonly CategoryTreeNodeDto[];
}

export interface ProductListItemDto {
  id: string;
  categoryId: string;
  categoryNameAr: string;
  categoryNameEn: string;
  nameAr: string;
  nameEn: string;
  barcode: string | null;
  sku: string | null;
  price: number;
  imageUrl: string | null;
  stockQuantity: number;
  /** When false, POS ignores stock. Defaults to true if omitted. */
  tracksInventory?: boolean;
  isActive: boolean;
}

export interface ProductDto {
  id: string;
  storeId: string;
  categoryId: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  barcode: string | null;
  sku: string | null;
  price: number;
  imageUrl: string | null;
  stockQuantity: number;
  tracksInventory?: boolean;
  isActive: boolean;
}

export interface ProductFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
}

export interface CreateProductPayload {
  categoryId: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  barcode?: string | null;
  sku?: string | null;
  price: number;
  imageUrl?: string | null;
  stockQuantity: number;
  tracksInventory: boolean;
  isActive?: boolean;
}

export type UpdateProductPayload = CreateProductPayload;

export interface CreateCategoryPayload {
  parentCategoryId?: string | null;
  nameAr: string;
  nameEn: string;
  isActive?: boolean;
}

export type UpdateCategoryPayload = Omit<CreateCategoryPayload, 'isActive'> & { isActive: boolean };

export interface DiscountDto {
  id: string;
  storeId: string;
  nameAr: string;
  nameEn: string;
  discountPercent: number;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
  appliesToAllProducts: boolean;
  categoryId: string | null;
  categoryNameAr: string | null;
  categoryNameEn: string | null;
  isActive: boolean;
  productIds: readonly string[];
  productCount: number;
}

export interface CreateDiscountPayload {
  nameAr: string;
  nameEn: string;
  discountPercent: number;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  appliesToAllProducts: boolean;
  categoryId?: string | null;
  isActive?: boolean;
  productIds?: readonly string[] | null;
}

export type UpdateDiscountPayload = CreateDiscountPayload & { isActive: boolean };
