import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { provideBrowserGlobalErrorListeners } from '@angular/core';
import { ApplicationConfig } from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
} from '@angular/router';

import { routes } from './app.routes';
import { httpErrorInterceptor } from './core/http/http-error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
        anchorScrolling: 'enabled',
      }),
      // Route parameters arrive as component inputs, so a component never reads the router
      // snapshot itself and stays straightforward to test with a plain input value.
      withComponentInputBinding(),
    ),
    provideHttpClient(
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN',
      }),
      withInterceptors([httpErrorInterceptor]),
    ),
  ],
};
