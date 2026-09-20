import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import { AuditLogFilterParams, AuditLogListItemDto } from '../../shared/models/audit-log.models';
import { unwrapApiResponse, toHttpParams } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class AuditLogsApiService {
  private readonly http = inject(HttpClient);

  getPaged(params: AuditLogFilterParams) {
    const q = toHttpParams({
      page: params.page,
      pageSize: params.pageSize,
      search: params.search,
      entityType: params.entityType,
      action: params.action,
      performedByUserId: params.performedByUserId,
      fromOccurredOnUtc: params.fromOccurredOnUtc,
      toOccurredOnUtc: params.toOccurredOnUtc,
    });
    return this.http
      .get<ApiResponse<PagedResult<AuditLogListItemDto>>>(apiUrl('/api/audit-logs'), { params: q })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getById(id: string) {
    return this.http
      .get<ApiResponse<AuditLogListItemDto>>(apiUrl(`/api/audit-logs/${id}`))
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
