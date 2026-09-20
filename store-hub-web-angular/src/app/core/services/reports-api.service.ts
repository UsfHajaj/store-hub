import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse } from '../../shared/models/api.types';
import {
  AdminReportSummaryDto,
  ReportRangeParams,
  StoreReportSummaryDto,
} from '../../shared/models/reports.models';
import { toHttpParams, unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);

  getStoreSummary(storeId: string, range: ReportRangeParams = {}) {
    return this.http
      .get<ApiResponse<StoreReportSummaryDto>>(apiUrl(`/api/stores/${storeId}/reports/summary`), {
        params: this.rangeParams(range),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getAdminSummary(range: ReportRangeParams = {}) {
    return this.http
      .get<ApiResponse<AdminReportSummaryDto>>(apiUrl('/api/reports/admin-summary'), {
        params: this.rangeParams(range),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  exportStoreExcel(storeId: string, range: ReportRangeParams = {}) {
    return this.http.get(apiUrl(`/api/stores/${storeId}/reports/summary/excel`), {
      params: this.rangeParams(range),
      responseType: 'blob',
    });
  }

  exportAdminExcel(range: ReportRangeParams = {}) {
    return this.http.get(apiUrl('/api/reports/admin-summary/excel'), {
      params: this.rangeParams(range),
      responseType: 'blob',
    });
  }

  private rangeParams(range: ReportRangeParams) {
    return toHttpParams({ fromUtc: range.fromUtc ?? null, toUtc: range.toUtc ?? null });
  }
}
