import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of, switchMap, tap, throwError } from 'rxjs';

import { isUnauthorizedError } from '../errors/problem-details';
import { AntiforgeryService } from './antiforgery.service';
import { AuthApiService } from './auth-api.service';
import { AuthMessageResponse, CurrentUser, LoginRequest, RegisterRequest } from './auth.models';
import { AuthStore } from './auth.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly antiforgery = inject(AntiforgeryService);
  private readonly authStore = inject(AuthStore);

  register(request: RegisterRequest): Observable<AuthMessageResponse> {
    return this.antiforgery.ensureToken().pipe(
      switchMap(() => this.authApi.register(request)),
    );
  }

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.antiforgery.ensureToken().pipe(
      switchMap(() => this.authApi.login(request)),
      switchMap(() => this.authStore.refreshCurrentUser()),
    );
  }

  logout(): Observable<void> {
    return this.antiforgery.ensureToken().pipe(
      switchMap(() => this.authApi.logout()),
      catchError((error: unknown) => {
        if (isUnauthorizedError(error)) {
          return of(undefined);
        }

        return throwError(() => error);
      }),
      tap(() => {
        this.antiforgery.reset();
      }),
      finalize(() => {
        this.authStore.clearPrivateState();
      }),
      map(() => undefined),
    );
  }
}
