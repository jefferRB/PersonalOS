import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { CurrentUser } from '../../core/auth/auth.models';
import { AuthStore } from '../../core/auth/auth.store';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { DEFAULT_CALENDAR_DISPLAY, UserProfile } from '../../core/profile/profile.models';
import { BROWSER_TIME_ZONE } from '../../core/time/browser-time-zones';
import { SettingsComponent } from './settings.component';

describe('SettingsComponent', () => {
  let fixture: ComponentFixture<SettingsComponent>;
  let http: HttpTestingController;
  let store: AuthStore;
  let router: Router;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  const savedProfile: UserProfile = {
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
    timeZoneId: 'UTC',
    calendarDisplay: DEFAULT_CALENDAR_DISPLAY,
    updatedAtUtc: '2026-07-30T19:24:00+00:00',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(
          withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
          withInterceptors([httpErrorInterceptor]),
        ),
        provideHttpClientTesting(),
        // Pinned so the suite never depends on the machine's own time zone.
        { provide: BROWSER_TIME_ZONE, useValue: 'America/Costa_Rica' },
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
    store.setAuthenticated(user);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(SettingsComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-theme-preference');
    document.documentElement.removeAttribute('style');
  });

  it('shows an accessible loading state before the profile arrives', () => {
    const status = query<HTMLElement>('[role="status"][aria-busy="true"]');

    expect(status.textContent).toContain('Loading your profile');
    expect(status.getAttribute('aria-busy')).toBe('true');

    flushProfile();
  });

  it('loads the authenticated profile into the form', () => {
    flushProfile({ ...savedProfile, displayName: 'Jefferson Rojas', timeZoneId: 'Europe/Madrid' });

    expect(displayNameInput().value).toBe('Jefferson Rojas');
    expect(timeZoneInput().value).toBe('Europe/Madrid');
    expect(query<HTMLInputElement>('#settings-email').value).toBe('jefferson@example.com');
    expect(fixture.nativeElement.textContent).toContain('Europe/Madrid');
  });

  it('renders an accessible error state and can retry when loading fails', () => {
    http.expectOne('/api/profile').flush(
      { title: 'Server error.', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );
    fixture.detectChanges();

    const alert = query<HTMLElement>('[role="alert"]');
    expect(alert.textContent).toContain('PersonalOS could not complete the request');

    queryAll<HTMLButtonElement>('button')
      .find((button) => button.textContent?.includes('Try again'))
      ?.click();
    fixture.detectChanges();

    flushProfile();

    expect(displayNameInput().value).toBe('Jefferson');
  });

  it('keeps the email field read-only', () => {
    flushProfile();

    const email = query<HTMLInputElement>('#settings-email');

    expect(email.readOnly).toBe(true);
    expect(email.disabled).toBe(true);
    expect(email.value).toBe('jefferson@example.com');
    expect(fixture.nativeElement.textContent).toContain(
      'Changing your email is not available yet',
    );
  });

  it('rejects a whitespace-only display name on the client', () => {
    flushProfile();

    setValue(displayNameInput(), '   ');
    submit();

    expect(fixture.nativeElement.textContent).toContain('Display name is required.');
    http.expectNone('/api/profile');
  });

  it('rejects a display name that is too short', () => {
    flushProfile();

    setValue(displayNameInput(), 'J');
    submit();

    expect(fixture.nativeElement.textContent).toContain('at least 2 characters');
    http.expectNone('/api/profile');
  });

  it('detects the browser time zone as a suggestion', () => {
    flushProfile();

    expect(fixture.nativeElement.textContent).toContain('Detected in this browser');
    expect(fixture.nativeElement.textContent).toContain('America/Costa_Rica');
  });

  it('shows the appearance choices with System selected by default', () => {
    flushProfile();

    expect(themeInput('system').checked).toBe(true);
    expect(themeInput('light').checked).toBe(false);
    expect(themeInput('dark').checked).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('PersonalOS is using the light interface');
    expect(localStorage.length).toBe(0);
  });

  it('persists an explicit dark appearance preference without using session storage', () => {
    flushProfile();

    themeInput('dark').click();
    fixture.detectChanges();

    expect(themeInput('dark').checked).toBe(true);
    expect(localStorage.getItem('personalos.themePreference')).toBe('dark');
    expect(sessionStorage.length).toBe(0);
    expect(document.documentElement.getAttribute('data-theme-preference')).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('never saves the browser suggestion on its own', () => {
    flushProfile();

    // The saved zone is still UTC even though the browser reports America/Costa_Rica.
    expect(timeZoneInput().value).toBe('UTC');
    expect(saveButton().disabled).toBe(true);
    http.expectNone('/api/profile');
  });

  it('fills the form when the browser time zone is applied, without saving', () => {
    flushProfile();

    useBrowserTimeZoneButton().click();
    fixture.detectChanges();

    expect(timeZoneInput().value).toBe('America/Costa_Rica');
    expect(saveButton().disabled).toBe(false);
    http.expectNone('/api/profile');
  });

  it('disables saving while the form matches the saved values', () => {
    flushProfile();

    expect(saveButton().disabled).toBe(true);

    setValue(displayNameInput(), 'Jefferson Rojas');

    expect(saveButton().disabled).toBe(false);

    setValue(displayNameInput(), 'Jefferson');

    expect(saveButton().disabled).toBe(true);
  });

  it('treats a whitespace-only edit as no change', () => {
    flushProfile();

    setValue(displayNameInput(), '  Jefferson  ');

    expect(saveButton().disabled).toBe(true);
  });

  it('prevents a duplicate submission while a save is in flight', () => {
    flushProfile();

    setValue(displayNameInput(), 'Jefferson Rojas');
    submit();

    const antiforgery = http.expectOne('/api/antiforgery/token');
    antiforgery.flush({ requestToken: 'request-token' });

    const firstSave = http.expectOne('/api/profile');

    submit();
    submit();

    // Still exactly one in-flight update.
    http.expectNone('/api/profile');

    firstSave.flush({ ...savedProfile, displayName: 'Jefferson Rojas' });
    fixture.detectChanges();
  });

  it('sends the profile update through the existing antiforgery flow', () => {
    flushProfile();

    setValue(displayNameInput(), 'Jefferson Rojas');
    setValue(timeZoneInput(), 'America/Costa_Rica');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });

    const request = http.expectOne('/api/profile');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      displayName: 'Jefferson Rojas',
      timeZoneId: 'America/Costa_Rica',
    });

    request.flush({
      ...savedProfile,
      displayName: 'Jefferson Rojas',
      timeZoneId: 'America/Costa_Rica',
    });
    fixture.detectChanges();
  });

  it('trims the display name before sending it', () => {
    flushProfile();

    setValue(displayNameInput(), '   Jefferson Rojas   ');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    const request = http.expectOne('/api/profile');

    expect(request.request.body.displayName).toBe('Jefferson Rojas');

    request.flush({ ...savedProfile, displayName: 'Jefferson Rojas' });
    fixture.detectChanges();
  });

  it('shows accessible success feedback and updates the page after saving', () => {
    flushProfile();
    saveProfile({
      ...savedProfile,
      displayName: 'Jefferson Rojas',
      timeZoneId: 'America/Costa_Rica',
    });

    const success = queryAll<HTMLElement>('[role="status"]').find((element) =>
      element.textContent?.includes('Settings saved'),
    );

    expect(success).toBeDefined();
    expect(success?.getAttribute('aria-live')).toBe('polite');
    expect(displayNameInput().value).toBe('Jefferson Rojas');
    expect(fixture.nativeElement.textContent).toContain('America/Costa_Rica');
  });

  it('updates the in-memory current user so the header and Today greeting react', () => {
    flushProfile();
    saveProfile({ ...savedProfile, displayName: 'Jefferson Rojas' });

    expect(store.currentUser()?.displayName).toBe('Jefferson Rojas');
    expect(store.currentUser()?.email).toBe('jefferson@example.com');
    expect(store.status()).toBe('authenticated');
  });

  it('resets the unsaved-changes state after a successful save', () => {
    flushProfile();

    setValue(displayNameInput(), 'Jefferson Rojas');
    expect(component().hasUnsavedChanges()).toBe(true);

    submit();
    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush({ ...savedProfile, displayName: 'Jefferson Rojas' });
    fixture.detectChanges();

    expect(component().hasUnsavedChanges()).toBe(false);
    expect(saveButton().disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('No unsaved changes');
  });

  it('maps a server time-zone error to the time-zone field', () => {
    flushProfile();

    setValue(timeZoneInput(), 'Not/AZone');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(
      {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { timeZoneId: ['Select a supported IANA time zone.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    const fieldError = query<HTMLElement>('#settings-time-zone-error');

    expect(fieldError.textContent).toContain('Select a supported IANA time zone.');
    expect(timeZoneInput().getAttribute('aria-invalid')).toBe('true');
    expect(query<HTMLElement>('#settings-display-name-error', true)).toBeNull();
  });

  it('maps a server display-name error to the display-name field', () => {
    flushProfile();

    setValue(displayNameInput(), 'Jefferson Rojas');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(
      {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { displayName: ['Display name must be between 2 and 100 characters.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect(query<HTMLElement>('#settings-display-name-error').textContent).toContain(
      'Display name must be between 2 and 100 characters.',
    );
  });

  it('renders server validation messages as text rather than markup', () => {
    flushProfile();

    setValue(timeZoneInput(), 'Not/AZone');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(
      {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { timeZoneId: ['<img src=x onerror="alert(1)">Rejected.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    const fieldError = query<HTMLElement>('#settings-time-zone-error');

    expect(fieldError.textContent).toContain('<img src=x onerror="alert(1)">Rejected.');
    expect(fieldError.querySelector('img')).toBeNull();
  });

  it('displays a rate-limit response safely', () => {
    flushProfile();

    setValue(displayNameInput(), 'Jefferson Rojas');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(
      { title: 'Too many requests.', status: 429, detail: 'Too many attempts. Try again later.' },
      { status: 429, statusText: 'Too Many Requests' },
    );
    fixture.detectChanges();

    const alert = queryAll<HTMLElement>('[role="alert"]').find((element) =>
      element.textContent?.includes('Too many attempts'),
    );

    expect(alert).toBeDefined();
    expect(alert?.textContent).toContain('Too many attempts');
    // The user's edit is preserved so the save can be retried.
    expect(displayNameInput().value).toBe('Jefferson Rojas');
  });

  it('handles a conflict response without losing the form', () => {
    flushProfile();

    setValue(displayNameInput(), 'Jefferson Rojas');
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(
      {
        title: 'Profile changed elsewhere.',
        status: 409,
        detail: 'The profile changed in another session. Reload and try again.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('changed in another session');
    expect(displayNameInput().value).toBe('Jefferson Rojas');
  });

  it('writes no profile data to localStorage', () => {
    flushProfile({ ...savedProfile, timeZoneId: 'America/Costa_Rica' });
    saveProfile({
      ...savedProfile,
      displayName: 'Jefferson Rojas',
      timeZoneId: 'America/Costa_Rica',
    });

    const contents = readStorage(localStorage);

    expect(localStorage.length).toBe(0);
    expect(contents).not.toContain('Jefferson');
    expect(contents).not.toContain('jefferson@example.com');
    expect(contents).not.toContain('America/Costa_Rica');
  });

  it('writes no profile data to sessionStorage', () => {
    flushProfile({ ...savedProfile, timeZoneId: 'America/Costa_Rica' });
    saveProfile({
      ...savedProfile,
      displayName: 'Jefferson Rojas',
      timeZoneId: 'America/Costa_Rica',
    });

    const contents = readStorage(sessionStorage);

    expect(sessionStorage.length).toBe(0);
    expect(contents).not.toContain('Jefferson');
    expect(contents).not.toContain('America/Costa_Rica');
  });

  it('asks before signing out with unsaved changes and stays when declined', () => {
    flushProfile();
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

    setValue(displayNameInput(), 'Jefferson Rojas');
    signOutButton().click();
    fixture.detectChanges();

    expect(confirmSpy).toHaveBeenCalled();
    expect(store.status()).toBe('authenticated');
    http.expectNone('/api/auth/logout');
  });

  it('signs out without asking when there are no unsaved changes', () => {
    flushProfile();
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    signOutButton().click();
    fixture.detectChanges();

    expect(confirmSpy).not.toHaveBeenCalled();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });

    expect(store.status()).toBe('anonymous');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('does not ask twice when the user already confirmed signing out', () => {
    flushProfile();
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    setValue(displayNameInput(), 'Jefferson Rojas');
    signOutButton().click();
    fixture.detectChanges();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });

    expect(component().hasUnsavedChanges()).toBe(false);
  });

  function component(): SettingsComponent {
    return fixture.componentInstance;
  }

  function flushProfile(profile: UserProfile = savedProfile): void {
    http.expectOne('/api/profile').flush(profile);
    fixture.detectChanges();
  }

  function saveProfile(profile: UserProfile): void {
    setValue(displayNameInput(), profile.displayName);
    setValue(timeZoneInput(), profile.timeZoneId);
    submit();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/profile').flush(profile);
    fixture.detectChanges();
  }

  function submit(): void {
    query<HTMLFormElement>('form').dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
    fixture.detectChanges();
  }

  function setValue(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('blur', { bubbles: true }));
    fixture.detectChanges();
  }

  function displayNameInput(): HTMLInputElement {
    return query<HTMLInputElement>('#settings-display-name');
  }

  function timeZoneInput(): HTMLInputElement {
    return query<HTMLInputElement>('#settings-time-zone');
  }

  function themeInput(value: string): HTMLInputElement {
    return query<HTMLInputElement>(`input[name="settings-theme"][value="${value}"]`);
  }

  function saveButton(): HTMLButtonElement {
    return query<HTMLButtonElement>('button[type="submit"]');
  }

  function useBrowserTimeZoneButton(): HTMLButtonElement {
    const button = queryAll<HTMLButtonElement>('button').find((candidate) =>
      candidate.textContent?.includes('Use browser time zone'),
    );
    expect(button).toBeDefined();

    return button as HTMLButtonElement;
  }

  function signOutButton(): HTMLButtonElement {
    return query<HTMLButtonElement>('.settings__sign-out');
  }

  function readStorage(storage: Storage): string {
    return JSON.stringify({ ...storage });
  }

  function query<T extends HTMLElement>(selector: string, allowMissing = false): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;

    if (!allowMissing) {
      expect(element).not.toBeNull();
    }

    return element as T;
  }

  function queryAll<T extends HTMLElement>(selector: string): T[] {
    return [...fixture.nativeElement.querySelectorAll(selector)] as T[];
  }
});
