import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { AuthMessageResponse, RegisterRequest } from '../../../core/auth/auth.models';
import { ApiError } from '../../../core/errors/problem-details';
import { RegisterComponent } from './register.component';

class AuthServiceStub {
  registerRequests: RegisterRequest[] = [];
  registerResponse: Observable<AuthMessageResponse> = of({ code: 'AccountCreated' });

  register(request: RegisterRequest): Observable<AuthMessageResponse> {
    this.registerRequests.push(request);
    return this.registerResponse;
  }
}

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let authService: AuthServiceStub;
  let router: Router;

  beforeEach(async () => {
    authService = new AuthServiceStub();

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(RegisterComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows accessible validation errors and password-confirmation feedback', () => {
    fillValidRegistration();
    setInput('#register-confirm-password', 'Password456');

    submitForm();

    expect(pageText()).toContain('Review the highlighted fields and try again.');
    expect(pageText()).toContain('Passwords must match.');
    expect(query<HTMLInputElement>('#register-confirm-password').getAttribute('aria-invalid')).toBe(
      'true',
    );
  });

  it('prevents duplicate submissions while registration is pending', () => {
    const pendingRegistration = new Subject<AuthMessageResponse>();
    authService.registerResponse = pendingRegistration.asObservable();
    fillValidRegistration();

    submitForm();
    submitForm();

    expect(authService.registerRequests).toHaveLength(1);
    pendingRegistration.next({ code: 'AccountCreated' });
    pendingRegistration.complete();
  });

  it('redirects to login with a safe success state after registration', () => {
    fillValidRegistration();

    submitForm();

    expect(authService.registerRequests[0]).toEqual({
      displayName: 'Jefferson',
      email: 'jefferson@example.com',
      password: 'Password123',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      state: { registrationCreated: true },
    });
  });

  it('maps safe server validation errors to fields and clears password fields', () => {
    authService.registerResponse = throwError(() =>
      ({
        status: 400,
        category: 'validation',
        title: 'One or more validation errors occurred.',
        detail: 'Validation failed.',
        validationErrors: {
          email: ['An account with this email already exists.'],
        },
        retryAfter: null,
        traceId: null,
      }) satisfies ApiError,
    );
    fillValidRegistration();

    submitForm();

    expect(pageText()).toContain('An account with this email already exists.');
    expect(query<HTMLInputElement>('#register-password').value).toBe('');
    expect(query<HTMLInputElement>('#register-confirm-password').value).toBe('');
  });

  function fillValidRegistration(): void {
    setInput('#register-display-name', 'Jefferson');
    setInput('#register-email', 'jefferson@example.com');
    setInput('#register-password', 'Password123');
    setInput('#register-confirm-password', 'Password123');
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
