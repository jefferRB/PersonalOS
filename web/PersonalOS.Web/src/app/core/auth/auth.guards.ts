import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthStore } from './auth.store';

export const authenticatedGuard: CanActivateFn = (_route, state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  return authStore.initialize().pipe(
    map((snapshot) =>
      snapshot.status === 'authenticated'
        ? true
        : router.createUrlTree(['/login'], {
            queryParams: state.url === '/app/today' ? undefined : { returnUrl: state.url },
          }),
    ),
  );
};

export const anonymousOnlyGuard: CanActivateFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  return authStore.initialize().pipe(
    map((snapshot) =>
      snapshot.status === 'authenticated' ? router.createUrlTree(['/app/today']) : true,
    ),
  );
};
