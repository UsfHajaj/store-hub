export type StoreType = 1 | 2 | 3 | 4 | 5 | 99;

export interface StoreListItemDto {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  storeType: StoreType;
  isActive: boolean;
  memberCount: number;
}

export interface StoreDto {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  storeType: StoreType;
  isActive: boolean;
  ownerUserId?: string | null;
  invoiceDisplayNameAr?: string | null;
  invoiceDisplayNameEn?: string | null;
  logoUrl?: string | null;
  taxNumber?: string | null;
  invoiceFooter?: string | null;
  memberUserIds: readonly string[];
}

export interface UpdateInvoiceSettingsPayload {
  invoiceDisplayNameAr?: string | null;
  invoiceDisplayNameEn?: string | null;
  taxNumber?: string | null;
  invoiceFooter?: string | null;
  logoUrl?: string | null;
}

export interface StoreMemberDto {
  userId: string;
  userName: string;
  nameAr?: string | null;
  nameEn?: string | null;
  email: string;
}

export interface CreateStorePayload {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  storeType: StoreType;
  memberUserIds: readonly string[];
}

export interface UpdateStorePayload {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  storeType: StoreType;
}

export const STORE_TYPE_OPTIONS: { value: StoreType; labelKey: string }[] = [
  { value: 1, labelKey: 'stores.type.cafe' },
  { value: 2, labelKey: 'stores.type.restaurant' },
  { value: 3, labelKey: 'stores.type.clothing' },
  { value: 4, labelKey: 'stores.type.cosmetics' },
  { value: 5, labelKey: 'stores.type.nuts' },
  { value: 99, labelKey: 'stores.type.other' },
];
