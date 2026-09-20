import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { safeReturnUrl } from '../auth/auth-navigation.util';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;

  const returnUrl = safeReturnUrl(state.url);
  void router.navigate(['/login'], {
    queryParams: returnUrl ? { returnUrl } : {},
    replaceUrl: true,
  });
  return false;
};
