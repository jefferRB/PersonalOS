import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { CurrentUser, LoginRequest } from '../../../core/auth/auth.models';
import { ApiError } from '../../../core/errors/problem-details';
import { LoginComponent } from './login.component';

const user: CurrentUser = {
  id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
  displayName: 'Jefferson',
  email: 'jefferson@example.com',
};

class AuthServiceStub {
  loginRequests: LoginRequest[] = [];
  loginResponse: Observable<CurrentUser> = of(user);

  login(request: LoginRequest): Observable<CurrentUser> {
    this.loginRequests.push(request);
    return this.loginResponse;
  }
}

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let authService: AuthServiceStub;
  let router: Router;

  beforeEach(async () => {
    authService = new AuthServiceStub();

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows accessible validation errors for an empty submit', () => {
    submitForm();

    const text = pageText();
    expect(text).toContain('Review the highlighted fields and try again.');
    expect(text).toContain('Email is required.');
    expect(text).toContain('Password is required.');
    expect(query<HTMLInputElement>('#login-email').getAttribute('aria-invalid')).toBe('true');
  });

  it('prevents duplicate submissions while login is pending', () => {
    const pendingLogin = new Subject<CurrentUser>();
    authService.loginResponse = pendingLogin.asObservable();
    fillValidLogin();

    submitForm();
    submitForm();

    expect(authService.loginRequests).toHaveLength(1);
    pendingLogin.next(user);
    pendingLogin.complete();
  });

  it('navigates to Today after a successful login', () => {
    fillValidLogin();

    submitForm();

    expect(authService.loginRequests[0]).toEqual({
      email: 'jefferson@example.com',
      password: 'Password123',
      rememberMe: false,
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/app/today');
  });

  it('keeps login failure generic and clears the password field', () => {
    authService.loginResponse = throwError(() =>
      apiError({
        status: 401,
        category: 'unauthorized',
        title: 'Invalid credentials.',
        detail: 'The email or password is incorrect.',
      }),
    );
    fillValidLogin();

    submitForm();

    expect(pageText()).toContain('The email or password is incorrect.');
    expect(query<HTMLInputElement>('#login-password').value).toBe('');
  });

  it('presents rate-limit responses clearly', () => {
    authService.loginResponse = throwError(() =>
      apiError({
        status: 429,
        category: 'rateLimit',
        title: 'Too many requests.',
        detail: 'Too many attempts. Try again later.',
      }),
    );
    fillValidLogin();

    submitForm();

    expect(pageText()).toContain('Too many attempts. Wait a moment and try again.');
  });

  it('toggles password visibility with an accessible button state', () => {
    const passwordInput = query<HTMLInputElement>('#login-password');
    query<HTMLButtonElement>('button[aria-label="Show password"]').click();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('text');
    expect(query<HTMLButtonElement>('button[aria-label="Hide password"]').getAttribute('aria-pressed')).toBe(
      'true',
    );
  });

  function fillValidLogin(): void {
    setInput('#login-email', 'jefferson@example.com');
    setInput('#login-password', 'Password123');
  }

  function submitForm(): void {
    query<HTMLFormElement>('form').dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
    fixture.detectChanges();
  }

  function setInput(selector: string, value: string): void {
    const input = query<HTMLInputElement>(selector);
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
  }

  function query<T extends HTMLElement>(selector: string): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;
    expect(element).not.toBeNull();

    return element as T;
  }

  function pageText(): string {
    return fixture.nativeElement.textContent ?? '';
  }
});

function apiError(options: {
  status: number;
  category: ApiError['category'];
  title: string;
  detail: string;
}): ApiError {
  return {
    status: options.status,
    category: options.category,
    title: options.title,
    detail: options.detail,
    validationErrors: {},
    retryAfter: null,
    traceId: null,
  };
}
