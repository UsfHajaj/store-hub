export interface UserListItemDto {
  id: string;
  userName: string;
  nameAr: string | null;
  nameEn: string | null;
  email: string;
  isActive: boolean;
  lastLoginUtc: string | null;
}

export interface UserFilterParams {
  page: number;
  pageSize: number;
  search?: string;
}

export interface UserDto extends UserListItemDto {
  roleNames: readonly string[];
  roleNamesEn?: readonly string[];
  roleIds: readonly string[];
}

export interface CreateUserPayload {
  userName: string;
  nameAr?: string | null;
  nameEn?: string | null;
  email: string;
  password: string;
  roleIds: readonly string[];
}

export interface UpdateUserPayload {
  userName: string;
  nameAr?: string | null;
  nameEn?: string | null;
  email: string;
  newPassword?: string | null;
}
