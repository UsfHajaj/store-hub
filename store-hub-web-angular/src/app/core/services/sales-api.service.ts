import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import {
  CreateSalePayload,
  CreateSaleReturnPayload,
  SaleDto,
  SaleFilterParams,
  SaleListItemDto,
  SaleReturnDto,
} from '../../shared/models/sales.models';
import { toHttpParams, unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class SalesApiService {
  private readonly http = inject(HttpClient);

  create(storeId: string, payload: CreateSalePayload) {
    return this.http
      .post<ApiResponse<SaleDto>>(apiUrl(`/api/stores/${storeId}/sales`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getPaged(storeId: string, filter: SaleFilterParams = {}) {
    const params = toHttpParams({
      page: filter.page ?? null,
      pageSize: filter.pageSize ?? null,
      search: filter.search?.trim() ?? null,
      fromUtc: filter.fromUtc ?? null,
      toUtc: filter.toUtc ?? null,
    });
    return this.http
      .get<ApiResponse<PagedResult<SaleListItemDto>>>(apiUrl(`/api/stores/${storeId}/sales`), { params })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(storeId: string, saleId: string) {
    return this.http
      .get<ApiResponse<SaleDto>>(apiUrl(`/api/stores/${storeId}/sales/${saleId}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  /** Returns the raw PDF so callers can open it in a new tab. */
  downloadInvoicePdf(storeId: string, saleId: string) {
    return this.http.get(apiUrl(`/api/stores/${storeId}/sales/${saleId}/invoice.pdf`), {
      responseType: 'blob',
    });
  }

  createReturn(storeId: string, saleId: string, payload: CreateSaleReturnPayload) {
    return this.http
      .post<ApiResponse<SaleReturnDto>>(apiUrl(`/api/stores/${storeId}/sales/${saleId}/returns`), payload)
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
