import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { LocaleService } from '../i18n/locale.service';
import { ErrorDisplayService } from '../services/error-display.service';
import { resolveUserFacingError } from '../../shared/utils/http-error.util';
import { isGuestAuthRoute, safeReturnUrl } from '../auth/auth-navigation.util';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const errors = inject(ErrorDisplayService);
  const locale = inject(LocaleService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse)) {
        return throwError(() => err);
      }

      const loginCall = req.url.includes('/api/auth/login');
      if (err.status === 401 && !loginCall) {
        auth.logout(false);
        // Avoid redirect loop: /login?returnUrl=/login?returnUrl=...
        if (!isGuestAuthRoute(router.url)) {
          const returnUrl = safeReturnUrl(router.url);
          void router.navigate(['/login'], {
            queryParams: returnUrl ? { returnUrl } : {},
            replaceUrl: true,
          });
        }
      } else if (!loginCall) {
        errors.show(resolveUserFacingError(err, (key) => locale.t(key)));
      }

      return throwError(() => err);
    }),
  );
};
