import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CreateUserPayload,
  UpdateUserPayload,
  UserDto,
  UserFilterParams,
  UserListItemDto,
} from '../../shared/models/user.models';
import { RoleListItemDto } from '../../shared/models/role.models';
import { unwrapApiResponse, toHttpParams } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);

  getRoleOptions() {
    return this.http
      .get<ApiResponse<RoleListItemDto[]>>(apiUrl('/api/users/role-options'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPaged(params: UserFilterParams) {
    const q = toHttpParams({
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
    });
    return this.http
      .get<ApiResponse<PagedResult<UserListItemDto>>>(apiUrl('/api/users'), { params: q })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<UserDto>>(apiUrl(`/api/users/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(payload: CreateUserPayload) {
    return this.http
      .post<ApiResponse<UserDto>>(apiUrl('/api/users'), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, payload: UpdateUserPayload) {
    return this.http
      .put<ApiResponse<UserDto>>(apiUrl(`/api/users/${id}`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  assignRoles(id: string, roleIds: readonly string[]) {
    return this.http
      .put<ApiResponse<UserDto>>(apiUrl(`/api/users/${id}/roles`), { roleIds })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  delete(id: string) {
    return this.http
      .delete<ApiResponse<unknown>>(apiUrl(`/api/users/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  activate(id: string) {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl(`/api/users/${id}/activate`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  deactivate(id: string) {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl(`/api/users/${id}/deactivate`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
