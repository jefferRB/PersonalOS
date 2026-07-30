import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { AuthSnapshot, CurrentUser } from './auth.models';
import { AuthStore } from './auth.store';
import { httpErrorInterceptor } from '../http/http-error.interceptor';

describe('AuthStore', () => {
  let http: HttpTestingController;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('starts in loading state and authenticates after /me succeeds', () => {
    const store = TestBed.inject(AuthStore);
    let snapshot: AuthSnapshot | undefined;

    store.initialize().subscribe((value) => {
      snapshot = value;
    });

    expect(store.status()).toBe('loading');

    http.expectOne('/api/auth/me').flush(user);

    expect(store.status()).toBe('authenticated');
    expect(store.currentUser()).toEqual(user);
    expect(snapshot?.user).toEqual(user);
  });

  it('treats /me 401 as anonymous without a generic error', () => {
    const store = TestBed.inject(AuthStore);
    let snapshot: AuthSnapshot | undefined;

    store.initialize().subscribe((value) => {
      snapshot = value;
    });

    http.expectOne('/api/auth/me').flush(
      { title: 'Unauthorized.', status: 401 },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(store.status()).toBe('anonymous');
    expect(store.currentUser()).toBeNull();
    expect(snapshot?.status).toBe('anonymous');
  });
});
