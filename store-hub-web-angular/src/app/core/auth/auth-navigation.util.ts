/** Guest-only auth routes that must never be used as returnUrl. */
const GUEST_AUTH_PATHS = ['/login', '/register', '/change-password'] as const;

export function pathOnly(url: string): string {
  const raw = (url || '/').trim();
  const noHash = raw.split('#')[0] ?? raw;
  const noQuery = noHash.split('?')[0] ?? noHash;
  return noQuery || '/';
}

export function isGuestAuthRoute(url: string): boolean {
  const p = pathOnly(url);
  return GUEST_AUTH_PATHS.some((route) => p === route || p.startsWith(`${route}/`));
}

/**
 * Safe post-login redirect target. Returns path only (no query) to avoid
 * nested returnUrl=/login?returnUrl=... loops that blow up the URL.
 */
export function safeReturnUrl(url: string | null | undefined): string | null {
  if (!url?.trim()) return null;
  if (url.length > 400) return null;

  const p = pathOnly(url);
  if (!p.startsWith('/') || p.startsWith('//')) return null;
  if (isGuestAuthRoute(p)) return null;
  if (p === '/select-store') return null;

  return p;
}
