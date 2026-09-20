import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CreateRolePayload,
  PermissionDto,
  RoleDto,
  RoleListItemDto,
  UpdateRolePayload,
} from '../../shared/models/role.models';
import { unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly http = inject(HttpClient);

  getRoles() {
    return this.http
      .get<ApiResponse<RoleListItemDto[]>>(apiUrl('/api/roles'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPaged(filter: { page?: number; pageSize?: number; search?: string } = {}) {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', String(filter.page));
    if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    return this.http
      .get<ApiResponse<PagedResult<RoleListItemDto>>>(apiUrl('/api/roles/paged'), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getRole(id: string) {
    return this.http
      .get<ApiResponse<RoleDto>>(apiUrl(`/api/roles/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  createRole(payload: CreateRolePayload) {
    return this.http
      .post<ApiResponse<RoleDto>>(apiUrl('/api/roles'), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  updateRole(id: string, payload: UpdateRolePayload) {
    return this.http
      .put<ApiResponse<RoleDto>>(apiUrl(`/api/roles/${id}`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  deleteRole(id: string) {
    return this.http
      .delete<ApiResponse<unknown>>(apiUrl(`/api/roles/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  setPermissions(id: string, permissionIds: readonly string[]) {
    return this.http
      .put<ApiResponse<RoleDto>>(apiUrl(`/api/roles/${id}/permissions`), { permissionIds })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPermissions() {
    return this.http
      .get<ApiResponse<PermissionDto[]>>(apiUrl('/api/permissions'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
