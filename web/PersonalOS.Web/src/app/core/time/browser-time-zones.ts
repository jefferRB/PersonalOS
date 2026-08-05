import { InjectionToken } from '@angular/core';

/**
 * Time zones offered when the browser cannot enumerate the IANA database.
 *
 * The list is only a convenience for the picker. The server remains the authority on which
 * identifiers are acceptable.
 */
const FALLBACK_TIME_ZONES: readonly string[] = [
  'UTC',
  'America/Costa_Rica',
  'America/Bogota',
  'America/Mexico_City',
  'America/Chicago',
  'America/Denver',
  'America/Los_Angeles',
  'America/New_York',
  'America/Sao_Paulo',
  'Europe/London',
  'Europe/Madrid',
  'Europe/Berlin',
  'Africa/Cairo',
  'Asia/Dubai',
  'Asia/Kolkata',
  'Asia/Shanghai',
  'Asia/Tokyo',
  'Australia/Sydney',
  'Pacific/Auckland',
];

/**
 * Reads the time zone the browser believes it is in.
 *
 * This is a suggestion only. It is never saved without an explicit user action, because the
 * browser reports the device's current location rather than where the account belongs.
 *
 * @returns The detected IANA identifier, or `null` when the browser cannot report one.
 */
export function detectBrowserTimeZone(): string | null {
  try {
    const detected = Intl.DateTimeFormat().resolvedOptions().timeZone;

    return typeof detected === 'string' && detected.trim().length > 0 ? detected : null;
  } catch {
    return null;
  }
}

/**
 * The browser's suggested time zone, resolved once per application.
 *
 * Detection is exposed through dependency injection so components stay testable: a test provides
 * a fixed value instead of depending on the machine that runs the suite.
 */
export const BROWSER_TIME_ZONE = new InjectionToken<string | null>('BROWSER_TIME_ZONE', {
  providedIn: 'root',
  factory: detectBrowserTimeZone,
});

/**
 * Lists the IANA identifiers the browser supports, falling back to a curated list.
 *
 * `Intl.supportedValuesOf` is not available in every browser, so the fallback keeps the picker
 * usable instead of leaving it empty.
 */
export function listSupportedTimeZones(): readonly string[] {
  const intl = Intl as typeof Intl & {
    supportedValuesOf?: (key: string) => string[];
  };

  if (typeof intl.supportedValuesOf === 'function') {
    try {
      const values = intl.supportedValuesOf('timeZone');

      if (Array.isArray(values) && values.length > 0) {
        return values;
      }
    } catch {
      // Fall through to the curated list below.
    }
  }

  return FALLBACK_TIME_ZONES;
}

/**
 * Builds the option list for the time-zone picker.
 *
 * The saved zone and the browser suggestion are always included, even when they are missing from
 * the browser's own list, so the user never sees their current setting disappear.
 */
export function buildTimeZoneOptions(
  savedTimeZoneId: string | null,
  browserTimeZoneId: string | null,
): readonly string[] {
  const options = new Set<string>(['UTC']);

  for (const candidate of [savedTimeZoneId, browserTimeZoneId]) {
    if (candidate !== null && candidate.trim().length > 0) {
      options.add(candidate);
    }
  }

  for (const zone of listSupportedTimeZones()) {
    options.add(zone);
  }

  return [...options].sort((left, right) => left.localeCompare(right, 'en'));
}
