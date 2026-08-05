import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { CurrentUser } from '../../auth/auth.models';
import { AuthStore } from '../../auth/auth.store';
import { httpErrorInterceptor } from '../../http/http-error.interceptor';
import { ApplicationShellComponent } from './application-shell.component';

describe('ApplicationShellComponent', () => {
  let fixture: ComponentFixture<ApplicationShellComponent>;
  let http: HttpTestingController;
  let router: Router;
  let store: AuthStore;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson Rojas',
    email: 'jefferson@example.com',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicationShellComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    store = TestBed.inject(AuthStore);
    store.setAuthenticated(user);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(ApplicationShellComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    http.verify();
  });

  it('shows the current user and exposes skip navigation', () => {
    const text = fixture.nativeElement.textContent ?? '';

    expect(text).toContain('Jefferson Rojas');
    expect(text).toContain('jefferson@example.com');
    expect(query<HTMLAnchorElement>('.skip-link').getAttribute('href')).toBe('#main-content');
  });

  it('updates the header when the saved display name changes', () => {
    store.updateDisplayName('Jefferson A Rojas');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent ?? '';

    expect(text).toContain('Jefferson A Rojas');
    expect(text).not.toContain('Jefferson Rojas ');
    // Initials are derived from the new name.
    expect(query<HTMLElement>('.avatar').textContent).toBe('JA');
  });

  it('clears private state and redirects after logout', () => {
    query<HTMLButtonElement>('.topbar__logout').click();
    fixture.detectChanges();

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });

    expect(store.status()).toBe('anonymous');
    expect(store.currentUser()).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });

  function query<T extends HTMLElement>(selector: string): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;
    expect(element).not.toBeNull();

    return element as T;
  }
});
