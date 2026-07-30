import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideLocationMocks } from '@angular/common/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { routes } from '../../app.routes';
import { CurrentUser } from './auth.models';
import { AuthStore } from './auth.store';
import { httpErrorInterceptor } from '../http/http-error.interceptor';

describe('authentication route guards', () => {
  let http: HttpTestingController;
  let router: Router;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideLocationMocks(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    http.verify();
  });

  it('redirects anonymous users away from protected routes', async () => {
    TestBed.inject(AuthStore).clearPrivateState();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/app/today');

    expect(router.url).toBe('/login');
  });

  it('redirects authenticated users away from anonymous-only routes', async () => {
    TestBed.inject(AuthStore).setAuthenticated(user);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/login');

    expect(router.url).toBe('/app/today');
  });

  it('renders the accessible not-found route for unknown URLs', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/missing-route');

    expect(router.url).toBe('/missing-route');
    expect(harness.routeNativeElement?.textContent).toContain('Page not found');
  });
});
