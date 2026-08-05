import { Component, ElementRef, ViewChild, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, take } from 'rxjs';

import { ThemePreference, ThemeService } from '../../core/appearance/theme.service';
import { AuthService } from '../../core/auth/auth.service';
import { AuthStore } from '../../core/auth/auth.store';
import {
  ApiError,
  firstValidationError,
  formLevelMessage,
  toApiError,
} from '../../core/errors/problem-details';
import { trimmedLength } from '../../core/forms/validators';
import { UnsavedChangesAware } from '../../core/navigation/unsaved-changes.guard';
import { UserProfile } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import { BROWSER_TIME_ZONE, buildTimeZoneOptions } from '../../core/time/browser-time-zones';

type ProfileForm = {
  displayName: FormControl<string>;
  timeZoneId: FormControl<string>;
};

interface ProfileFormValue {
  readonly displayName: string;
  readonly timeZoneId: string;
}

const DISCARD_CHANGES_MESSAGE = 'You have unsaved changes. Sign out and discard them?';
const THEME_OPTIONS: ReadonlyArray<{
  readonly value: ThemePreference;
  readonly label: string;
  readonly description: string;
}> = [
  {
    value: 'system',
    label: 'System',
    description: 'Follow this device.',
  },
  {
    value: 'light',
    label: 'Light',
    description: 'Use the light interface.',
  },
  {
    value: 'dark',
    label: 'Dark',
    description: 'Use the dark interface.',
  },
];

@Component({
  selector: 'app-settings',
  imports: [ReactiveFormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements UnsavedChangesAware {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly authService = inject(AuthService);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);

  @ViewChild('errorSummary') private errorSummary?: ElementRef<HTMLElement>;

  protected readonly form = this.formBuilder.group<ProfileForm>({
    displayName: this.formBuilder.control('', [trimmedLength(2, 100)]),
    timeZoneId: this.formBuilder.control('', [trimmedLength(1, 100)]),
  });

  protected readonly currentUser = this.authStore.currentUser;
  protected readonly themeOptions = THEME_OPTIONS;
  protected readonly themePreference = this.themeService.preference;
  protected readonly resolvedTheme = this.themeService.resolvedTheme;
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly isSigningOut = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly saveSuccess = signal(false);
  protected readonly serverFieldErrors = signal<Record<string, string[]>>({});
  protected readonly profile = signal<UserProfile | null>(null);

  /** Time zone the browser reports. It is a suggestion and is never saved on its own. */
  protected readonly browserTimeZoneId = signal<string | null>(inject(BROWSER_TIME_ZONE));

  private readonly baseline = signal<ProfileFormValue | null>(null);
  private readonly currentValue = signal<ProfileFormValue>({ displayName: '', timeZoneId: '' });
  private readonly isLeavingIntentionally = signal(false);

  protected readonly timeZoneOptions = computed(() =>
    buildTimeZoneOptions(this.profile()?.timeZoneId ?? null, this.browserTimeZoneId()),
  );

  protected readonly savedTimeZoneId = computed(() => this.profile()?.timeZoneId ?? null);

  protected readonly canUseBrowserTimeZone = computed(() => {
    const suggestion = this.browserTimeZoneId();

    return suggestion !== null && suggestion !== this.currentValue().timeZoneId;
  });

  /** True when the form holds edits that have not been saved yet. */
  protected readonly hasChanges = computed(() => {
    const base = this.baseline();

    if (base === null) {
      return false;
    }

    const value = this.currentValue();

    return (
      value.displayName.trim() !== base.displayName.trim()
      || value.timeZoneId.trim() !== base.timeZoneId.trim()
    );
  });

  protected readonly canSubmit = computed(
    () => !this.isLoading() && !this.isSaving() && this.hasChanges(),
  );

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.currentValue.set(this.form.getRawValue());
      this.serverFieldErrors.set({});
      this.formError.set(null);
      this.saveSuccess.set(false);
    });

    this.loadProfile();
  }

  /** Used by the route guard to protect unsaved edits during navigation. */
  hasUnsavedChanges(): boolean {
    return this.hasChanges() && !this.isLeavingIntentionally();
  }

  protected loadProfile(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.profileService
      .getProfile()
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (profile) => this.applySavedProfile(profile),
        error: (error: unknown) => {
          this.loadError.set(formLevelMessage(toApiError(error)));
        },
      });
  }

  /** Copies the browser suggestion into the form without saving it. */
  protected useBrowserTimeZone(): void {
    const suggestion = this.browserTimeZoneId();

    if (suggestion === null) {
      return;
    }

    this.form.controls.timeZoneId.setValue(suggestion);
    this.form.controls.timeZoneId.markAsDirty();
    this.form.controls.timeZoneId.markAsTouched();
  }

  protected selectThemePreference(preference: ThemePreference): void {
    this.themeService.setPreference(preference);
  }

  protected onSubmit(): void {
    // A save already in flight ignores further submissions.
    if (this.isSaving() || !this.canSubmit()) {
      return;
    }

    this.formError.set(null);
    this.serverFieldErrors.set({});
    this.saveSuccess.set(false);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Review the highlighted fields and try again.');
      this.focusSummary();

      return;
    }

    this.isSaving.set(true);
    const value = this.form.getRawValue();

    this.profileService
      .updateProfile({
        displayName: value.displayName.trim(),
        timeZoneId: value.timeZoneId.trim(),
      })
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: (profile) => {
          this.applySavedProfile(profile);
          this.saveSuccess.set(true);
        },
        error: (error: unknown) => this.applyServerError(error),
      });
  }

  protected signOut(): void {
    if (this.isSigningOut()) {
      return;
    }

    if (this.hasChanges() && !window.confirm(DISCARD_CHANGES_MESSAGE)) {
      return;
    }

    // The decision was made here, so the route guard must not ask a second time.
    this.isLeavingIntentionally.set(true);
    this.isSigningOut.set(true);

    this.authService
      .logout()
      .pipe(
        take(1),
        finalize(() => this.isSigningOut.set(false)),
      )
      .subscribe({
        next: () => this.router.navigateByUrl('/login'),
        error: () => this.router.navigateByUrl('/login'),
      });
  }

  protected displayNameError(): string | null {
    const serverError = firstValidationError(this.serverFieldErrors(), 'displayName');

    if (serverError !== null) {
      return serverError;
    }

    const control = this.form.controls.displayName;

    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'Display name is required.';
    }

    if (control.hasError('minlength')) {
      return 'Display name must be at least 2 characters.';
    }

    if (control.hasError('maxlength')) {
      return 'Display name must be 100 characters or fewer.';
    }

    return null;
  }

  protected timeZoneError(): string | null {
    const serverError = firstValidationError(this.serverFieldErrors(), 'timeZoneId');

    if (serverError !== null) {
      return serverError;
    }

    const control = this.form.controls.timeZoneId;

    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'Time zone is required.';
    }

    if (control.hasError('maxlength')) {
      return 'Time zone must be 100 characters or fewer.';
    }

    return null;
  }

  protected describedBy(...ids: Array<string | null>): string | null {
    const joinedIds = ids.filter((id): id is string => id !== null).join(' ');

    return joinedIds.length > 0 ? joinedIds : null;
  }

  private applySavedProfile(profile: UserProfile): void {
    this.profile.set(profile);
    this.authStore.updateDisplayName(profile.displayName);

    const value: ProfileFormValue = {
      displayName: profile.displayName,
      timeZoneId: profile.timeZoneId,
    };

    // Saved data becomes the new baseline, so the form reports itself as clean again.
    this.form.setValue(value, { emitEvent: false });
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.currentValue.set(value);
    this.baseline.set(value);
  }

  private applyServerError(error: unknown): void {
    const apiError: ApiError = toApiError(error);

    this.serverFieldErrors.set(apiError.validationErrors);
    this.formError.set(formLevelMessage(apiError));
    this.form.markAllAsTouched();
    this.focusSummary();
  }

  private focusSummary(): void {
    queueMicrotask(() => this.errorSummary?.nativeElement.focus());
  }
}
