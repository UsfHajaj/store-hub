import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of, catchError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { ActiveStoreService } from '../services/active-store.service';
import { StoresApiService } from '../services/stores-api.service';

/** Ensures store list is loaded; redirects to picker when multiple stores and none selected. */
export const storeContextGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const active = inject(ActiveStoreService);
  const api = inject(StoresApiService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login'], { queryParams: {} });
  }

  return api.getMine().pipe(
    map((list) => {
      active.stores.set(list);
      if (list.length === 0) {
        return true;
      }
      const current = active.activeStoreId();
      if (current && list.some((s) => s.id === current)) {
        return true;
      }
      if (list.length === 1) {
        active.selectStore(list[0].id);
        return true;
      }
      return router.createUrlTree(['/select-store']);
    }),
    catchError(() => of(true)),
  );
};
