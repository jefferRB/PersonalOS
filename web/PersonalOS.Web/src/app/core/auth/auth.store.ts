import { computed, Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';

import { isUnauthorizedError } from '../errors/problem-details';
import { AuthApiService } from './auth-api.service';
import { AuthSnapshot, AuthStatus, CurrentUser } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly authApi = inject(AuthApiService);
  private readonly statusSignal = signal<AuthStatus>('unknown');
  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private readonly startupResolvedSignal = signal(false);
  private startupRequest: Observable<AuthSnapshot> | null = null;

  readonly status = this.statusSignal.asReadonly();
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.statusSignal() === 'authenticated');
  readonly isAnonymous = computed(() => this.statusSignal() === 'anonymous');
  readonly isStartupLoading = computed(
    () => !this.startupResolvedSignal() && this.statusSignal() !== 'anonymous',
  );

  initialize(): Observable<AuthSnapshot> {
    if (this.startupResolvedSignal()) {
      return of(this.snapshot());
    }

    if (this.startupRequest) {
      return this.startupRequest;
    }

    this.statusSignal.set('loading');

    this.startupRequest = this.authApi.getCurrentUser().pipe(
      tap((user) => this.setAuthenticated(user)),
      map(() => this.snapshot()),
      catchError((error: unknown) => {
        this.clearPrivateState();

        if (!isUnauthorizedError(error)) {
          this.statusSignal.set('anonymous');
        }

        return of(this.snapshot());
      }),
      finalize(() => {
        this.startupResolvedSignal.set(true);
        this.startupRequest = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.startupRequest;
  }

  refreshCurrentUser(): Observable<CurrentUser> {
    this.statusSignal.set('loading');

    return this.authApi.getCurrentUser().pipe(
      tap((user) => {
        this.setAuthenticated(user);
        this.startupResolvedSignal.set(true);
      }),
      catchError((error: unknown) => {
        if (isUnauthorizedError(error)) {
          this.clearPrivateState();
        }

        throw error;
      }),
    );
  }

  setAuthenticated(user: CurrentUser): void {
    this.currentUserSignal.set(user);
    this.statusSignal.set('authenticated');
    this.startupResolvedSignal.set(true);
  }

  /**
   * Applies a saved display-name change to the in-memory current user.
   *
   * This keeps one current-user store rather than introducing a competing profile store. The
   * change stays in memory: `/api/auth/me` remains the source of truth after a reload, and
   * nothing is written to browser storage.
   */
  updateDisplayName(displayName: string): void {
    const user = this.currentUserSignal();

    if (user === null || user.displayName === displayName) {
      return;
    }

    this.currentUserSignal.set({ ...user, displayName });
  }

  clearPrivateState(): void {
    this.currentUserSignal.set(null);
    this.statusSignal.set('anonymous');
    this.startupResolvedSignal.set(true);
  }

  snapshot(): AuthSnapshot {
    return {
      status: this.statusSignal(),
      user: this.currentUserSignal(),
    };
  }
}
