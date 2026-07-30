import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';
import { AuthStore } from './core/auth/auth.store';
import { httpErrorInterceptor } from './core/http/http-error.interceptor';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('shows startup loading while current-user state is unresolved', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Checking your secure session');
    expect(fixture.nativeElement.textContent).not.toContain('Good day');

    http.expectOne('/api/auth/me');
  });

  it('moves to anonymous state after an expected current-user 401', () => {
    const fixture = TestBed.createComponent(App);
    const store = TestBed.inject(AuthStore);
    fixture.detectChanges();

    http.expectOne('/api/auth/me').flush(
      { title: 'Unauthorized.', status: 401 },
      { status: 401, statusText: 'Unauthorized' },
    );
    fixture.detectChanges();

    expect(store.status()).toBe('anonymous');
    expect(store.currentUser()).toBeNull();
  });
});
