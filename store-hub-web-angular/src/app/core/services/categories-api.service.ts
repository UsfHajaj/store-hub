import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CategoryDto,
  CategoryFilterParams,
  CategoryTreeNodeDto,
  CreateCategoryPayload,
  UpdateCategoryPayload,
} from '../../shared/models/catalog.models';
import { unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class CategoriesApiService {
  private readonly http = inject(HttpClient);

  getList(storeId: string, tree = false) {
    let params = new HttpParams();
    if (tree) {
      params = params.set('tree', 'true');
    }
    return this.http
      .get<ApiResponse<CategoryDto[] | CategoryTreeNodeDto[]>>(
        apiUrl(`/api/stores/${storeId}/categories`),
        { params },
      )
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPaged(storeId: string, filter: CategoryFilterParams = {}) {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', String(filter.page));
    if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    return this.http
      .get<ApiResponse<PagedResult<CategoryDto>>>(apiUrl(`/api/stores/${storeId}/categories/paged`), {
        params,
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(storeId: string, payload: CreateCategoryPayload) {
    return this.http
      .post<ApiResponse<CategoryDto>>(apiUrl(`/api/stores/${storeId}/categories`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(storeId: string, id: string, payload: UpdateCategoryPayload) {
    return this.http
      .put<ApiResponse<CategoryDto>>(apiUrl(`/api/stores/${storeId}/categories/${id}`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  delete(storeId: string, id: string) {
    return this.http
      .delete<ApiResponse<unknown>>(apiUrl(`/api/stores/${storeId}/categories/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
