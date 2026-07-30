import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AntiforgeryTokenResponse,
  AuthMessageResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  getAntiforgeryToken(): Observable<AntiforgeryTokenResponse> {
    return this.http.get<AntiforgeryTokenResponse>('/api/antiforgery/token');
  }

  getCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>('/api/auth/me');
  }

  register(request: RegisterRequest): Observable<AuthMessageResponse> {
    return this.http.post<AuthMessageResponse>('/api/auth/register', request);
  }

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.http.post<CurrentUser>('/api/auth/login', request);
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', null);
  }
}
