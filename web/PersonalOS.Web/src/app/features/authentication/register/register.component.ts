import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import {
  ApiError,
  firstValidationError,
  formLevelMessage,
  isApiError,
} from '../../../core/errors/problem-details';

type RegisterForm = {
  displayName: FormControl<string>;
  email: FormControl<string>;
  password: FormControl<string>;
  confirmPassword: FormControl<string>;
};

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  @ViewChild('errorSummary') private errorSummary?: ElementRef<HTMLElement>;

  protected readonly form = this.formBuilder.group<RegisterForm>(
    {
      displayName: this.formBuilder.control('', [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
      ]),
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
      confirmPassword: this.formBuilder.control('', [Validators.required]),
    },
    { validators: [matchingPasswordsValidator()] },
  );

  protected readonly showPassword = signal(false);
  protected readonly showConfirmation = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly serverFieldErrors = signal<Record<string, string[]>>({});

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.serverFieldErrors.set({});
      this.formError.set(null);
    });
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected toggleConfirmationVisibility(): void {
    this.showConfirmation.update((visible) => !visible);
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
      .register({
        displayName: value.displayName.trim(),
        email: value.email.trim(),
        password: value.password,
      })
      .pipe(
        take(1),
        finalize(() => this.isSubmitting.set(false)),
      )
      .subscribe({
        next: () => {
          this.router.navigate(['/login'], {
            state: { registrationCreated: true },
          });
        },
        error: (error: unknown) => {
          this.applyServerError(error);
        },
      });
  }

  protected displayNameError(): string | null {
    const serverError = firstValidationError(this.serverFieldErrors(), 'displayName');

    if (serverError) {
      return serverError;
    }

    if (!this.form.controls.displayName.touched) {
      return null;
    }

    if (this.form.controls.displayName.hasError('required')) {
      return 'Display name is required.';
    }

    if (this.form.controls.displayName.hasError('minlength')) {
      return 'Display name must be at least 2 characters.';
    }

    if (this.form.controls.displayName.hasError('maxlength')) {
      return 'Display name must be 100 characters or fewer.';
    }

    return null;
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

  protected confirmationError(): string | null {
    if (!this.form.controls.confirmPassword.touched) {
      return null;
    }

    if (this.form.controls.confirmPassword.hasError('required')) {
      return 'Confirm your password.';
    }

    if (this.form.hasError('passwordMismatch')) {
      return 'Passwords must match.';
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
    this.form.controls.confirmPassword.reset('', { emitEvent: false });
    this.focusSummary();
  }

  private focusSummary(): void {
    queueMicrotask(() => this.errorSummary?.nativeElement.focus());
  }
}

function matchingPasswordsValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('password')?.value;
    const confirmation = control.get('confirmPassword')?.value;

    return typeof password === 'string'
      && typeof confirmation === 'string'
      && password.length > 0
      && confirmation.length > 0
      && password !== confirmation
      ? { passwordMismatch: true }
      : null;
  };
}
