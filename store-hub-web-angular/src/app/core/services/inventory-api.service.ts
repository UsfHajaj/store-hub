import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  AdjustStockPayload,
  CreateStocktakePayload,
  InventoryFilterParams,
  InventoryItemDto,
  StocktakeDto,
  StocktakeListItemDto,
  UpdateStocktakeLinesPayload,
} from '../../shared/models/inventory.models';
import { toHttpParams, unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly http = inject(HttpClient);

  getPaged(storeId: string, filter: InventoryFilterParams = {}) {
    const params = toHttpParams({
      page: filter.page ?? null,
      pageSize: filter.pageSize ?? null,
      search: filter.search?.trim() ?? null,
      lowStockOnly: filter.lowStockOnly ? true : null,
    });
    return this.http
      .get<ApiResponse<PagedResult<InventoryItemDto>>>(apiUrl(`/api/stores/${storeId}/inventory`), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  adjust(storeId: string, payload: AdjustStockPayload) {
    return this.http
      .post<ApiResponse<InventoryItemDto>>(apiUrl(`/api/stores/${storeId}/inventory/adjust`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getStocktakes(storeId: string) {
    return this.http
      .get<ApiResponse<StocktakeListItemDto[]>>(apiUrl(`/api/stores/${storeId}/stocktakes`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getStocktakesPaged(storeId: string, filter: { page?: number; pageSize?: number } = {}) {
    const params = toHttpParams({
      page: filter.page ?? null,
      pageSize: filter.pageSize ?? null,
    });
    return this.http
      .get<ApiResponse<PagedResult<StocktakeListItemDto>>>(
        apiUrl(`/api/stores/${storeId}/stocktakes/paged`),
        { params },
      )
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getStocktake(storeId: string, stocktakeId: string) {
    return this.http
      .get<ApiResponse<StocktakeDto>>(apiUrl(`/api/stores/${storeId}/stocktakes/${stocktakeId}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  createStocktake(storeId: string, payload: CreateStocktakePayload = {}) {
    return this.http
      .post<ApiResponse<StocktakeDto>>(apiUrl(`/api/stores/${storeId}/stocktakes`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  updateStocktakeLines(storeId: string, stocktakeId: string, payload: UpdateStocktakeLinesPayload) {
    return this.http
      .put<ApiResponse<StocktakeDto>>(apiUrl(`/api/stores/${storeId}/stocktakes/${stocktakeId}/lines`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  completeStocktake(storeId: string, stocktakeId: string) {
    return this.http
      .post<ApiResponse<StocktakeDto>>(apiUrl(`/api/stores/${storeId}/stocktakes/${stocktakeId}/complete`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
