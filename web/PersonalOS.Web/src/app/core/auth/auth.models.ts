export type AuthStatus = 'unknown' | 'loading' | 'authenticated' | 'anonymous';

export interface CurrentUser {
  readonly id: string;
  readonly displayName: string;
  readonly email: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
  readonly rememberMe: boolean;
}

export interface RegisterRequest {
  readonly displayName: string;
  readonly email: string;
  readonly password: string;
}

export interface AuthMessageResponse {
  readonly code: string;
}

export interface AntiforgeryTokenResponse {
  readonly requestToken: string;
}

export interface AuthSnapshot {
  readonly status: AuthStatus;
  readonly user: CurrentUser | null;
}
