import { HttpErrorResponse } from '@angular/common/http';
import { ApiResponse } from '../models/api.types';
import { ApiBusinessError } from './api-helpers';

export type TranslateFn = (key: string) => string;

const TECHNICAL_PATTERNS: readonly RegExp[] = [
  /^Http failure response/i,
  /^Http failure/i,
  /Unknown Error/i,
  /^Error:\s*Http/i,
  /HttpErrorResponse/i,
  /^An unexpected error occurred\.?$/i,
  /^The requested API endpoint was not found\.?$/i,
  /^Internal Server Error$/i,
  /^Bad Gateway$/i,
  /^Service Unavailable$/i,
  /^Gateway Timeout$/i,
];

function isApiResponseBody(value: unknown): value is ApiResponse {
  return typeof value === 'object' && value !== null && 'success' in value;
}

function isTechnicalMessage(text: string): boolean {
  const trimmed = text.trim();
  if (!trimmed) return true;
  return TECHNICAL_PATTERNS.some((re) => re.test(trimmed));
}

function flattenUnknownErrors(value: unknown): string[] {
  if (!value) return [];
  if (typeof value === 'string') return value.trim() ? [value.trim()] : [];
  if (Array.isArray(value)) {
    return value.flatMap((item) => flattenUnknownErrors(item));
  }
  if (typeof value === 'object') {
    const obj = value as Record<string, unknown>;
    if (Array.isArray(obj['errors'])) {
      return flattenUnknownErrors(obj['errors']);
    }
    if (obj['errors'] && typeof obj['errors'] === 'object' && !Array.isArray(obj['errors'])) {
      return Object.values(obj['errors'] as Record<string, unknown>).flatMap((v) => flattenUnknownErrors(v));
    }
    if (typeof obj['title'] === 'string' && obj['title'].trim()) {
      return [obj['title'].trim()];
    }
    if (typeof obj['detail'] === 'string' && obj['detail'].trim()) {
      return [obj['detail'].trim()];
    }
    if (typeof obj['message'] === 'string' && obj['message'].trim()) {
      return [obj['message'].trim()];
    }
  }
  return [];
}

export function extractApiErrorMessages(err: unknown): string[] {
  if (err instanceof ApiBusinessError) {
    return err.errors.filter((e) => e?.trim()).map((e) => e.trim());
  }

  if (err instanceof HttpErrorResponse) {
    if (isApiResponseBody(err.error) && err.error.errors?.length) {
      return err.error.errors.filter((e): e is string => typeof e === 'string' && !!e.trim()).map((e) => e.trim());
    }
    return flattenUnknownErrors(err.error);
  }

  if (err instanceof Error && err.message.trim()) {
    return [err.message.trim()];
  }

  return [];
}

function messageForStatus(status: number, t: TranslateFn): string {
  if (status === 0) return t('errors.network');
  if (status === 400 || status === 422) return t('errors.validation');
  if (status === 401) return t('errors.unauthorized');
  if (status === 403) return t('errors.forbidden');
  if (status === 404) return t('errors.notFound');
  if (status === 408 || status === 504) return t('errors.timeout');
  if (status === 409) return t('errors.conflict');
  if (status === 429) return t('errors.rateLimited');
  if (status >= 500) return t('errors.server');
  return t('errors.generic');
}

/** User-facing message: prefers readable API errors, never raw browser HTTP text. */
export function resolveUserFacingError(err: unknown, t: TranslateFn): string {
  const apiMessages = extractApiErrorMessages(err).filter((m) => !isTechnicalMessage(m));
  if (apiMessages.length) {
    return apiMessages.join(' · ');
  }

  if (err instanceof HttpErrorResponse) {
    return messageForStatus(err.status, t);
  }

  return t('errors.generic');
}
