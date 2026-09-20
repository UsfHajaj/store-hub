import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse } from '../../shared/models/api.types';
import { MyPerformanceDto } from '../../shared/models/profile.models';
import { UserDto } from '../../shared/models/user.models';
import { toHttpParams, unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class ProfileApiService {
  private readonly http = inject(HttpClient);

  getMe() {
    return this.http
      .get<ApiResponse<UserDto>>(apiUrl('/api/profile/me'))
      .pipe(map((r) => unwrapApiResponse(r)));
  }

  getMyPerformance(storeId: string) {
    return this.http
      .get<ApiResponse<MyPerformanceDto>>(apiUrl('/api/profile/performance'), {
        params: toHttpParams({ storeId }),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
