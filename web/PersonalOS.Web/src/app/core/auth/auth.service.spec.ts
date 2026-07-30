import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth.service';
import { AuthStore } from './auth.store';
import { CurrentUser } from './auth.models';
import { httpErrorInterceptor } from '../http/http-error.interceptor';

describe('AuthService', () => {
  let http: HttpTestingController;
  let storageSpy: ReturnType<typeof vi.spyOn>;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        AuthStore,
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    storageSpy = vi.spyOn(Storage.prototype, 'setItem');
  });

  afterEach(() => {
    storageSpy.mockRestore();
    http.verify();
  });

  it('fetches antiforgery before login, refreshes /me, and keeps auth out of localStorage', () => {
    const service = TestBed.inject(AuthService);
    const store = TestBed.inject(AuthStore);
    let result: CurrentUser | undefined;

    service
      .login({
        email: ' jefferson@example.com ',
        password: 'Password123',
        rememberMe: true,
      })
      .subscribe((value) => {
        result = value;
      });

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });

    const loginRequest = http.expectOne('/api/auth/login');
    expect(loginRequest.request.method).toBe('POST');
    expect(loginRequest.request.body).toEqual({
      email: ' jefferson@example.com ',
      password: 'Password123',
      rememberMe: true,
    });
    loginRequest.flush(user);

    http.expectOne('/api/auth/me').flush(user);

    expect(result).toEqual(user);
    expect(store.currentUser()).toEqual(user);
    expect(storageSpy).not.toHaveBeenCalled();
  });

  it('registers with antiforgery without authenticating the in-memory user', () => {
    const service = TestBed.inject(AuthService);
    const store = TestBed.inject(AuthStore);
    let responseCode: string | undefined;

    service
      .register({
        displayName: 'Jefferson',
        email: 'jefferson@example.com',
        password: 'Password123',
      })
      .subscribe((response) => {
        responseCode = response.code;
      });

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/auth/register').flush({ code: 'AccountCreated' });

    expect(responseCode).toBe('AccountCreated');
    expect(store.status()).toBe('unknown');
    expect(store.currentUser()).toBeNull();
    expect(storageSpy).not.toHaveBeenCalled();
  });

  it('clears private state after logout', () => {
    const service = TestBed.inject(AuthService);
    const store = TestBed.inject(AuthStore);
    let completed = false;
    store.setAuthenticated(user);

    service.logout().subscribe(() => {
      completed = true;
    });

    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'request-token' });
    http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
    expect(store.status()).toBe('anonymous');
    expect(store.currentUser()).toBeNull();
  });
});
