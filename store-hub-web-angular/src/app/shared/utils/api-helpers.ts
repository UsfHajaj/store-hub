import { HttpParams } from '@angular/common/http';
import { ApiResponse } from '../models/api.types';

export class ApiBusinessError extends Error {
  constructor(
    public readonly errors: readonly string[],
    public readonly traceId?: string | null,
  ) {
    super(errors.join('; '));
    this.name = 'ApiBusinessError';
  }
}

export function unwrapApiResponse<T>(response: ApiResponse<T>): T {
  if (!response.success) {
    throw new ApiBusinessError(response.errors ?? ['Request failed'], response.traceId);
  }
  if (response.data === undefined || response.data === null) {
    return undefined as T;
  }
  return response.data;
}

export function toHttpParams(record: Record<string, string | number | boolean | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(record)) {
    if (value === undefined || value === null || value === '') continue;
    params = params.set(key, String(value));
  }
  return params;
}
