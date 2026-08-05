import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CurrentUser } from '../auth/auth.models';
import { AuthStore } from '../auth/auth.store';
import { httpErrorInterceptor } from '../http/http-error.interceptor';
import { DEFAULT_CALENDAR_DISPLAY, UserProfile } from './profile.models';
import { ProfileService } from './profile.service';

describe('ProfileService', () => {
  let http: HttpTestingController;
  let store: AuthStore;
  let profileService: ProfileService;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  const profile: UserProfile = {
    displayName: 'Jefferson Rojas',
    email: 'jefferson@example.com',
    timeZoneId: 'America/Costa_Rica',
    calendarDisplay: DEFAULT_CALENDAR_DISPLAY,
    updatedAtUtc: '2026-07-30T19:24:00+00:00',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
          withInterceptors([httpErrorInterceptor]),
        ),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
    profileService = TestBed.inject(ProfileService);
    store.setAuthenticated(user);
    localStorage.clear();
    sessionStorage.clear();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('requests an antiforgery token before updating the profile', () => {
    profileService
      .updateProfile({ displayName: 'Jefferson Rojas', timeZoneId: 'America/Costa_Rica' })
      .subscribe();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });

    const update = http.expectOne('/api/profile');

    expect(update.request.method).toBe('PUT');

    update.flush(profile);
  });

  it('does not send the update when the antiforgery request fails', () => {
    let failed = false;

    profileService
      .updateProfile({ displayName: 'Jefferson Rojas', timeZoneId: 'UTC' })
      .subscribe({ error: () => (failed = true) });

    http.expectOne('/api/antiforgery/token').flush(
      { title: 'Server error.', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );

    expect(failed).toBe(true);
    http.expectNone('/api/profile');
  });

  it('updates the in-memory current user after a successful save', () => {
    profileService
      .updateProfile({ displayName: 'Jefferson Rojas', timeZoneId: 'America/Costa_Rica' })
      .subscribe();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(profile);

    expect(store.currentUser()?.displayName).toBe('Jefferson Rojas');
    expect(store.currentUser()?.id).toBe(user.id);
    expect(store.currentUser()?.email).toBe(user.email);
  });

  it('leaves the current user untouched when the save fails', () => {
    profileService
      .updateProfile({ displayName: 'Jefferson Rojas', timeZoneId: 'Not/AZone' })
      .subscribe({ error: () => undefined });

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(
      { title: 'Validation failed.', status: 400, errors: { timeZoneId: ['Rejected.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(store.currentUser()?.displayName).toBe('Jefferson');
  });

  it('reads the profile without requiring an antiforgery token', () => {
    profileService.getProfile().subscribe();

    http.expectNone('/api/antiforgery/token');
    http.expectOne('/api/profile').flush(profile);
  });

  it('reads the time context without requiring an antiforgery token', () => {
    profileService.getTimeContext().subscribe();

    http.expectNone('/api/antiforgery/token');
    http.expectOne('/api/time/context').flush({
      utcNow: '2026-07-30T19:24:00+00:00',
      localNow: '2026-07-30T13:24:00-06:00',
      localDate: '2026-07-30',
      timeZoneId: 'America/Costa_Rica',
      utcOffsetMinutes: -360,
    });
  });

  it('keeps profile responses out of browser storage', () => {
    profileService.getProfile().subscribe();
    http.expectOne('/api/profile').flush(profile);

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
