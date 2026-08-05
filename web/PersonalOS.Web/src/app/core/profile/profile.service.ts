import { Injectable, inject } from '@angular/core';
import { Observable, switchMap, tap } from 'rxjs';

import { AntiforgeryService } from '../auth/antiforgery.service';
import { AuthStore } from '../auth/auth.store';
import { ProfileApiService } from './profile-api.service';
import {
  TimeContext,
  UpdateCalendarDisplayRequest,
  UpdateProfileRequest,
  UserProfile,
} from './profile.models';

/**
 * Profile operations for the authenticated account.
 *
 * The service reuses the existing antiforgery flow for the state-changing request and feeds the
 * saved display name back into the single in-memory current-user store, so the header and the
 * Today greeting react through signals without a page reload.
 */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly profileApi = inject(ProfileApiService);
  private readonly antiforgery = inject(AntiforgeryService);
  private readonly authStore = inject(AuthStore);

  getProfile(): Observable<UserProfile> {
    return this.profileApi.getProfile();
  }

  getTimeContext(): Observable<TimeContext> {
    return this.profileApi.getTimeContext();
  }

  updateProfile(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.antiforgery.ensureToken().pipe(
      switchMap(() => this.profileApi.updateProfile(request)),
      tap((profile) => this.authStore.updateDisplayName(profile.displayName)),
    );
  }

  /**
   * Saves how the day planner's timeline is shown.
   *
   * It goes through the same antiforgery flow as every other write. The display name is not fed
   * back into the current-user store, because this request cannot change it.
   */
  updateCalendarDisplay(request: UpdateCalendarDisplayRequest): Observable<UserProfile> {
    return this.antiforgery.ensureToken().pipe(
      switchMap(() => this.profileApi.updateCalendarDisplay(request)),
    );
  }
}
