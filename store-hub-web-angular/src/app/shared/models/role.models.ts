export interface PermissionDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  module: string;
}

export interface RoleListItemDto {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  isSystemRole: boolean;
  permissionCount: number;
  userCount: number;
}

export interface RoleDto {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  isSystemRole: boolean;
  permissionIds: readonly string[];
}

export interface CreateRolePayload {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  permissionIds: readonly string[];
}

export interface UpdateRolePayload {
  nameAr: string;
  nameEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
}
