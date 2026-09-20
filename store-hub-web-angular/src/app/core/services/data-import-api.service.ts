import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import { apiUrl } from '../config/api-url';
import { ApiResponse } from '../../shared/models/api.types';
import { DataImportKind, DataImportResultDto } from '../../shared/models/data-import.models';
import { toHttpParams, unwrapApiResponse } from '../../shared/utils/api-helpers';

@Injectable({ providedIn: 'root' })
export class DataImportApiService {
  private readonly http = inject(HttpClient);

  downloadTemplate(kind: DataImportKind, sample: boolean) {
    return this.http.get(apiUrl(`/api/data-import/templates/${kind}`), {
      params: toHttpParams({ sample }),
      responseType: 'blob',
    });
  }

  import(kind: DataImportKind, file: File, storeId?: string | null) {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http
      .post<ApiResponse<DataImportResultDto>>(apiUrl(`/api/data-import/${kind}`), form, {
        params: toHttpParams({ storeId: storeId ?? null }),
      })
      .pipe(map((r) => unwrapApiResponse(r)));
  }
}
