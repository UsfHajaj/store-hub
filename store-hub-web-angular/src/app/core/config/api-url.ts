import { environment } from '../../../environments/environment';

export function apiUrl(path: string): string {
  const normalized = path.startsWith('/') ? path : `/${path}`;
  const base = environment.apiBaseUrl.trim();
  if (!base) return normalized;
  return `${base.replace(/\/$/, '')}${normalized}`;
}

export function isSameAppApiRequest(url: string): boolean {
  if (url.startsWith('/api/')) return true;
  const base = environment.apiBaseUrl.trim();
  return !!base && url.startsWith(base);
}
