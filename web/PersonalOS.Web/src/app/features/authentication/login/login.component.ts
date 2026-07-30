import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import {
  ApiError,
  firstValidationError,
  formLevelMessage,
  isApiError,
} from '../../../core/errors/problem-details';

type LoginForm = {
  email: FormControl<string>;
  password: FormControl<string>;
  rememberMe: FormControl<boolean>;
};

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  @ViewChild('errorSummary') private errorSummary?: ElementRef<HTMLElement>;

  protected readonly form = this.formBuilder.group<LoginForm>({
    email: this.formBuilder.control('', [
      Validators.required,
      Validators.email,
      Validators.maxLength(254),
    ]),
    password: this.formBuilder.control('', [
      Validators.required,
      Validators.minLength(8),
      Validators.maxLength(128),
    ]),
    rememberMe: this.formBuilder.control(false),
  });

  protected readonly showPassword = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly serverFieldErrors = signal<Record<string, string[]>>({});
  protected readonly registrationSuccess = signal(false);

  constructor() {
    const state = this.router.getCurrentNavigation()?.extras.state ?? history.state;
    this.registrationSuccess.set(hasRegistrationCreatedState(state));

    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.serverFieldErrors.set({});
      this.formError.set(null);
      this.registrationSuccess.set(false);
    });
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected onSubmit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.formError.set(null);
    this.serverFieldErrors.set({});

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Review the highlighted fields and try again.');
      this.focusSummary();
      return;
    }

    this.isSubmitting.set(true);
    const value = this.form.getRawValue();

    this.authService
      .login({
        email: value.email.trim(),
        password: value.password,
        rememberMe: value.rememberMe,
      })
      .pipe(
        take(1),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.router.navigateByUrl(this.safeReturnUrl());
        },
        error: (error: unknown) => {
          this.applyServerError(error);
        },
      });
  }

  protected emailError(): string | null {
    const serverError = firstValidationError(this.serverFieldErrors(), 'email');

    if (serverError) {
      return serverError;
    }

    if (!this.form.controls.email.touched) {
      return null;
    }

    if (this.form.controls.email.hasError('required')) {
      return 'Email is required.';
    }

    if (this.form.controls.email.hasError('email')) {
      return 'Enter a valid email address.';
    }

    if (this.form.controls.email.hasError('maxlength')) {
      return 'Email must be 254 characters or fewer.';
    }

    return null;
  }

  protected passwordError(): string | null {
    const serverError = firstValidationError(this.serverFieldErrors(), 'password');

    if (serverError) {
      return serverError;
    }

    if (!this.form.controls.password.touched) {
      return null;
    }

    if (this.form.controls.password.hasError('required')) {
      return 'Password is required.';
    }

    if (this.form.controls.password.hasError('minlength')) {
      return 'Password must be at least 8 characters.';
    }

    if (this.form.controls.password.hasError('maxlength')) {
      return 'Password must be 128 characters or fewer.';
    }

    return null;
  }

  protected describedBy(...ids: Array<string | null>): string | null {
    const joinedIds = ids.filter((id): id is string => id !== null).join(' ');

    return joinedIds.length > 0 ? joinedIds : null;
  }

  private applyServerError(error: unknown): void {
    const apiError = isApiError(error)
      ? error
      : ({
          status: 0,
          category: 'unknown',
          title: 'Request failed.',
          detail: 'The request could not be completed.',
          validationErrors: {},
          retryAfter: null,
          traceId: null,
        } satisfies ApiError);

    this.serverFieldErrors.set(apiError.validationErrors);
    this.formError.set(formLevelMessage(apiError));
    this.form.controls.password.reset('', { emitEvent: false });
    this.focusSummary();
  }

  private safeReturnUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    return returnUrl?.startsWith('/app/') ? returnUrl : '/app/today';
  }

  private focusSummary(): void {
    queueMicrotask(() => this.errorSummary?.nativeElement.focus());
  }
}

function hasRegistrationCreatedState(value: unknown): boolean {
  return typeof value === 'object'
    && value !== null
    && 'registrationCreated' in value
    && value.registrationCreated === true;
}
