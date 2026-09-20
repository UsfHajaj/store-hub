import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CreateDiscountPayload,
  DiscountDto,
  UpdateDiscountPayload,
} from '../../shared/models/catalog.models';
import { unwrapApiResponse } from '../../shared/utils/api-helpers';

export interface DiscountFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class DiscountsApiService {
  private readonly http = inject(HttpClient);

  getAll(storeId: string) {
    return this.http
      .get<ApiResponse<DiscountDto[]>>(apiUrl(`/api/stores/${storeId}/discounts`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPaged(storeId: string, filter: DiscountFilterParams = {}) {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', String(filter.page));
    if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    return this.http
      .get<ApiResponse<PagedResult<DiscountDto>>>(apiUrl(`/api/stores/${storeId}/discounts/paged`), {
        params,
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(storeId: string, id: string) {
    return this.http
      .get<ApiResponse<DiscountDto>>(apiUrl(`/api/stores/${storeId}/discounts/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(storeId: string, payload: CreateDiscountPayload) {
    return this.http
      .post<ApiResponse<DiscountDto>>(apiUrl(`/api/stores/${storeId}/discounts`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(storeId: string, id: string, payload: UpdateDiscountPayload) {
    return this.http
      .put<ApiResponse<DiscountDto>>(apiUrl(`/api/stores/${storeId}/discounts/${id}`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  delete(storeId: string, id: string) {
    return this.http
      .delete<ApiResponse<unknown>>(apiUrl(`/api/stores/${storeId}/discounts/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
