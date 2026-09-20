import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse, PagedResult } from '../../shared/models/api.types';
import { NotificationFilterParams, NotificationListItemDto } from '../../shared/models/notification.models';
import { unwrapApiResponse, toHttpParams } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);

  getMy(params: NotificationFilterParams) {
    const q = toHttpParams({
      page: params.page,
      pageSize: params.pageSize,
      unreadOnly: params.unreadOnly === undefined ? undefined : params.unreadOnly,
    });
    return this.http
      .get<ApiResponse<PagedResult<NotificationListItemDto>>>(apiUrl('/api/notifications/my'), { params: q })
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getMyUnreadCount() {
    return this.http
      .get<ApiResponse<number>>(apiUrl('/api/notifications/my/unread-count'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  markRead(id: string) {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl(`/api/notifications/${id}/mark-read`), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  markAllMyRead() {
    return this.http
      .post<ApiResponse<unknown>>(apiUrl('/api/notifications/my/mark-all-read'), {})
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
