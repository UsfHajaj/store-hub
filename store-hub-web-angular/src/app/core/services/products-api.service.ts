import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CreateProductPayload,
  ProductDto,
  ProductFilterParams,
  ProductListItemDto,
  UpdateProductPayload,
} from '../../shared/models/catalog.models';
import { unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ProductsApiService {
  private readonly http = inject(HttpClient);

  getPaged(storeId: string, filter: ProductFilterParams = {}) {
    let params = new HttpParams();
    if (filter.page) params = params.set('page', String(filter.page));
    if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);

    return this.http
      .get<ApiResponse<PagedResult<ProductListItemDto>>>(apiUrl(`/api/stores/${storeId}/products`), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(storeId: string, id: string) {
    return this.http
      .get<ApiResponse<ProductDto>>(apiUrl(`/api/stores/${storeId}/products/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  create(storeId: string, payload: CreateProductPayload) {
    return this.http
      .post<ApiResponse<ProductDto>>(apiUrl(`/api/stores/${storeId}/products`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  update(storeId: string, id: string, payload: UpdateProductPayload) {
    return this.http
      .put<ApiResponse<ProductDto>>(apiUrl(`/api/stores/${storeId}/products/${id}`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  delete(storeId: string, id: string) {
    return this.http
      .delete<ApiResponse<unknown>>(apiUrl(`/api/stores/${storeId}/products/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  uploadImage(storeId: string, file: File) {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http
      .post<ApiResponse<{ imageUrl: string }>>(apiUrl(`/api/stores/${storeId}/products/upload-image`), body)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
