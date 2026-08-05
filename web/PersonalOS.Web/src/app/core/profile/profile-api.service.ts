import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  TimeContext,
  UpdateCalendarDisplayRequest,
  UpdateProfileRequest,
  UserProfile,
} from './profile.models';

/**
 * Typed access to the profile and time endpoints.
 *
 * Responses are held by callers in memory only. Nothing here writes to `localStorage`,
 * `sessionStorage`, or any other browser storage.
 */
@Injectable({ providedIn: 'root' })
export class ProfileApiService {
  private readonly http = inject(HttpClient);

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>('/api/profile');
  }

  updateProfile(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>('/api/profile', request);
  }

  /**
   * Saves how the day planner's timeline is shown.
   *
   * The calendar toolbar has its own endpoint so that changing an interval cannot overwrite the
   * display name or the time zone, which belong to the settings screen.
   */
  updateCalendarDisplay(request: UpdateCalendarDisplayRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>('/api/profile/calendar-display', request);
  }

  getTimeContext(): Observable<TimeContext> {
    return this.http.get<TimeContext>('/api/time/context');
  }
}
