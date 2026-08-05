import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-theme-preference');
    document.documentElement.removeAttribute('style');
    ensureColorSchemeMeta();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.unstubAllGlobals();
    localStorage.clear();
    sessionStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-theme-preference');
    document.documentElement.removeAttribute('style');
  });

  it('uses System by default without writing browser storage', () => {
    installMatchMedia(false);

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('system');
    expect(service.resolvedTheme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme-preference')).toBe('system');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(colorSchemeMeta().content).toBe('light dark');
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('loads a saved dark preference before any server state is needed', () => {
    localStorage.setItem('personalos.themePreference', 'dark');
    installMatchMedia(false);

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('dark');
    expect(service.resolvedTheme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(colorSchemeMeta().content).toBe('dark');
  });

  it('persists an explicit theme choice without using session storage', () => {
    installMatchMedia(true);
    const service = TestBed.inject(ThemeService);

    service.setPreference('light');

    expect(service.preference()).toBe('light');
    expect(service.resolvedTheme()).toBe('light');
    expect(localStorage.getItem('personalos.themePreference')).toBe('light');
    expect(sessionStorage.length).toBe(0);
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(colorSchemeMeta().content).toBe('light');
  });

  it('updates when the operating-system theme changes while System is selected', () => {
    const media = installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    expect(service.resolvedTheme()).toBe('light');

    media.setMatches(true);

    expect(service.preference()).toBe('system');
    expect(service.resolvedTheme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('ignores operating-system changes after a fixed theme is selected', () => {
    const media = installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    service.setPreference('light');
    media.setMatches(true);

    expect(service.preference()).toBe('light');
    expect(service.resolvedTheme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('treats an invalid stored value as System', () => {
    localStorage.setItem('personalos.themePreference', 'sepia');
    installMatchMedia(true);

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('system');
    expect(service.resolvedTheme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme-preference')).toBe('system');
  });

  function ensureColorSchemeMeta(): void {
    const existing = document.querySelector<HTMLMetaElement>('meta[name="color-scheme"]');

    if (existing !== null) {
      existing.content = 'light dark';
      return;
    }

    const meta = document.createElement('meta');
    meta.name = 'color-scheme';
    meta.content = 'light dark';
    document.head.append(meta);
  }

  function colorSchemeMeta(): HTMLMetaElement {
    const meta = document.querySelector<HTMLMetaElement>('meta[name="color-scheme"]');
    expect(meta).not.toBeNull();

    return meta as HTMLMetaElement;
  }

  function installMatchMedia(initialMatches: boolean): { setMatches: (matches: boolean) => void } {
    let matches = initialMatches;
    const listeners = new Set<(event: MediaQueryListEvent) => void>();
    const mediaQuery = {
      get matches() {
        return matches;
      },
      media: '(prefers-color-scheme: dark)',
      onchange: null,
      addEventListener: (_type: string, listener: EventListenerOrEventListenerObject | null) => {
        if (typeof listener === 'function') {
          listeners.add(listener as (event: MediaQueryListEvent) => void);
        }
      },
      removeEventListener: (
        _type: string,
        listener: EventListenerOrEventListenerObject | null,
      ) => {
        if (typeof listener === 'function') {
          listeners.delete(listener as (event: MediaQueryListEvent) => void);
        }
      },
      addListener: (listener: (event: MediaQueryListEvent) => void) => listeners.add(listener),
      removeListener: (listener: (event: MediaQueryListEvent) => void) =>
        listeners.delete(listener),
      dispatchEvent: (event: Event) => {
        for (const listener of listeners) {
          listener(event as MediaQueryListEvent);
        }

        return true;
      },
    } as MediaQueryList;

    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue(mediaQuery));

    return {
      setMatches(nextMatches: boolean): void {
        matches = nextMatches;
        mediaQuery.dispatchEvent({ matches: nextMatches } as MediaQueryListEvent);
      },
    };
  }
});
