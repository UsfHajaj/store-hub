import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CreateStorePayload,
  StoreDto,
  StoreListItemDto,
  StoreMemberDto,
  UpdateInvoiceSettingsPayload,
  UpdateStorePayload,
} from '../../shared/models/store.models';
import { unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class StoresApiService {
  private readonly http = inject(HttpClient);

  getMine() {
    return this.http
      .get<ApiResponse<StoreListItemDto[]>>(apiUrl('/api/stores/mine'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getAll() {
    return this.http
      .get<ApiResponse<StoreListItemDto[]>>(apiUrl('/api/stores'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPaged(filter: { page?: number; pageSize?: number; search?: string } = {}) {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', String(filter.page));
    if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    return this.http
      .get<ApiResponse<PagedResult<StoreListItemDto>>>(apiUrl('/api/stores/paged'), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<StoreDto>>(apiUrl(`/api/stores/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(payload: CreateStorePayload) {
    return this.http
      .post<ApiResponse<StoreDto>>(apiUrl('/api/stores'), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(id: string, payload: UpdateStorePayload) {
    return this.http
      .put<ApiResponse<StoreDto>>(apiUrl(`/api/stores/${id}`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  activate(id: string) {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl(`/api/stores/${id}/activate`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  deactivate(id: string) {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl(`/api/stores/${id}/deactivate`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getInvoiceSettings(id: string) {
    return this.http
      .get<ApiResponse<StoreDto>>(apiUrl(`/api/stores/${id}/invoice-settings`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  updateInvoiceSettings(id: string, payload: UpdateInvoiceSettingsPayload) {
    return this.http
      .put<ApiResponse<StoreDto>>(apiUrl(`/api/stores/${id}/invoice-settings`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  uploadLogo(id: string, file: File) {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http
      .post<ApiResponse<{ imageUrl: string }>>(apiUrl(`/api/stores/${id}/logo`), body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getMembers(id: string) {
    return this.http
      .get<ApiResponse<StoreMemberDto[]>>(apiUrl(`/api/stores/${id}/members`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  setMembers(id: string, userIds: readonly string[]) {
    return this.http
      .put<ApiResponse<StoreMemberDto[]>>(apiUrl(`/api/stores/${id}/members`), { userIds })
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
