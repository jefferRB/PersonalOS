import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthApiService } from './auth-api.service';
import { AntiforgeryTokenResponse } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AntiforgeryService {
  private readonly authApi = inject(AuthApiService);

  ensureToken(): Observable<AntiforgeryTokenResponse> {
    return this.authApi.getAntiforgeryToken();
  }

  reset(): void {
  }
}
