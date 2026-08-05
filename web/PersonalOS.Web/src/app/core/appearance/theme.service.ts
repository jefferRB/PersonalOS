import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'personalos.themePreference';
const DARK_QUERY = '(prefers-color-scheme: dark)';

const THEME_PREFERENCES: readonly ThemePreference[] = ['system', 'light', 'dark'];

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly darkModeQuery = createDarkModeQuery();

  private readonly preferenceSignal = signal<ThemePreference>(readStoredPreference());
  private readonly resolvedThemeSignal = signal<ResolvedTheme>(
    resolveTheme(this.preferenceSignal(), this.darkModeQuery),
  );

  readonly preference = this.preferenceSignal.asReadonly();
  readonly resolvedTheme = this.resolvedThemeSignal.asReadonly();
  readonly isSystem = computed(() => this.preferenceSignal() === 'system');

  constructor() {
    this.synchronize();
    this.watchSystemPreference();
  }

  synchronize(): void {
    this.applyTheme(this.preferenceSignal());
  }

  setPreference(preference: ThemePreference): void {
    if (!isThemePreference(preference)) {
      return;
    }

    this.preferenceSignal.set(preference);
    writeStoredPreference(preference);
    this.applyTheme(preference);
  }

  private watchSystemPreference(): void {
    if (this.darkModeQuery === null) {
      return;
    }

    const listener = (): void => {
      if (this.preferenceSignal() === 'system') {
        this.applyTheme('system');
      }
    };

    this.darkModeQuery.addEventListener('change', listener);
  }

  private applyTheme(preference: ThemePreference): void {
    const resolved = resolveTheme(preference, this.darkModeQuery);
    const root = this.document.documentElement;

    root.setAttribute('data-theme-preference', preference);
    root.setAttribute('data-theme', resolved);
    root.style.colorScheme = resolved;
    this.resolvedThemeSignal.set(resolved);
    this.updateColorSchemeMeta(preference, resolved);
  }

  private updateColorSchemeMeta(preference: ThemePreference, resolved: ResolvedTheme): void {
    const meta = this.document.querySelector<HTMLMetaElement>('meta[name="color-scheme"]');

    if (meta === null) {
      return;
    }

    meta.content = preference === 'system' ? 'light dark' : resolved;
  }
}

export function isThemePreference(value: unknown): value is ThemePreference {
  return typeof value === 'string' && THEME_PREFERENCES.includes(value as ThemePreference);
}

function readStoredPreference(): ThemePreference {
  try {
    return normalizePreference(globalThis.localStorage?.getItem(THEME_STORAGE_KEY));
  } catch {
    return 'system';
  }
}

function writeStoredPreference(preference: ThemePreference): void {
  try {
    globalThis.localStorage?.setItem(THEME_STORAGE_KEY, preference);
  } catch {
    // Storage can be blocked by browser policy. Theme still applies for the current page.
  }
}

function normalizePreference(value: string | null | undefined): ThemePreference {
  return isThemePreference(value) ? value : 'system';
}

function createDarkModeQuery(): MediaQueryList | null {
  if (typeof globalThis.matchMedia !== 'function') {
    return null;
  }

  return globalThis.matchMedia(DARK_QUERY);
}

function resolveTheme(
  preference: ThemePreference,
  darkModeQuery: MediaQueryList | null,
): ResolvedTheme {
  if (preference !== 'system') {
    return preference;
  }

  return darkModeQuery?.matches === true ? 'dark' : 'light';
}
