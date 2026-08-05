import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import { AuthApiService } from './auth-api.service';
import { AntiforgeryTokenResponse } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AntiforgeryService {
  private readonly authApi = inject(AuthApiService);

  ensureToken(): Observable<AntiforgeryTokenResponse> {
    return this.authApi.getAntiforgeryToken();
  }

  /**
   * Runs a state-changing request only after a fresh antiforgery token exists.
   *
   * Every feature service sends its writes through here, so a new endpoint cannot forget the
   * token. The token itself is never held in a field or written to browser storage: the server
   * sets the readable cookie and Angular's `HttpClient` copies it into the `X-XSRF-TOKEN` header
   * on the next request.
   *
   * @param request Factory that performs the write once the token is available.
   */
  protect<TResponse>(request: () => Observable<TResponse>): Observable<TResponse> {
    return this.ensureToken().pipe(switchMap(request));
  }

  reset(): void {
  }
}
