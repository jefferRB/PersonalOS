/**
 * PersonalOS renders dates in English regardless of the operating-system or browser language.
 *
 * The locale is stated explicitly instead of relying on the browser default, which would render
 * the date in Spanish on a Spanish machine. The formatter also pins `timeZone: 'UTC'` because the
 * value being formatted is a bare calendar date the server already resolved for the account: the
 * browser must not shift it again.
 */
const ENGLISH_DATE_FORMATTER = new Intl.DateTimeFormat('en-US', {
  weekday: 'long',
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  timeZone: 'UTC',
});

const LOCAL_DATE_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

/**
 * Formats the server-provided local calendar date in English.
 *
 * @param localDate Calendar date as `yyyy-MM-dd`, taken from the time-context response.
 * @returns For example `Thursday, July 30, 2026`, or `null` when the value is unusable.
 */
export function formatEnglishLocalDate(localDate: string | null | undefined): string | null {
  if (typeof localDate !== 'string') {
    return null;
  }

  const parts = LOCAL_DATE_PATTERN.exec(localDate);

  if (parts === null) {
    return null;
  }

  const year = Number(parts[1]);
  const month = Number(parts[2]);
  const day = Number(parts[3]);
  const value = new Date(Date.UTC(year, month - 1, day));

  if (Number.isNaN(value.getTime())) {
    return null;
  }

  // Reject values such as 2026-02-31, which JavaScript would silently roll over.
  if (
    value.getUTCFullYear() !== year ||
    value.getUTCMonth() !== month - 1 ||
    value.getUTCDate() !== day
  ) {
    return null;
  }

  return ENGLISH_DATE_FORMATTER.format(value);
}

/**
 * Formats a UTC offset in minutes as a readable label such as `UTC-06:00`.
 *
 * @param utcOffsetMinutes Offset supplied by the time-context response.
 */
export function formatUtcOffset(utcOffsetMinutes: number): string {
  if (!Number.isFinite(utcOffsetMinutes)) {
    return 'UTC';
  }

  const total = Math.trunc(utcOffsetMinutes);

  if (total === 0) {
    return 'UTC';
  }

  const sign = total < 0 ? '-' : '+';
  const absolute = Math.abs(total);
  const hours = Math.floor(absolute / 60)
    .toString()
    .padStart(2, '0');
  const minutes = (absolute % 60).toString().padStart(2, '0');

  return `UTC${sign}${hours}:${minutes}`;
}
