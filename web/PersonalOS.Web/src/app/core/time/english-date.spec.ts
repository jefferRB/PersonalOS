import { formatEnglishLocalDate, formatUtcOffset } from './english-date';

describe('formatEnglishLocalDate', () => {
  it('renders the server date in English', () => {
    expect(formatEnglishLocalDate('2026-07-30')).toBe('Thursday, July 30, 2026');
  });

  it('renders every month name in English', () => {
    expect(formatEnglishLocalDate('2026-01-05')).toContain('January');
    expect(formatEnglishLocalDate('2026-12-25')).toContain('December');
  });

  it('does not shift the date when the browser sits behind UTC', () => {
    // A naive `new Date('2026-07-30')` rendered in a negative-offset zone would show 29 July.
    expect(formatEnglishLocalDate('2026-07-30')).toContain('July 30, 2026');
  });

  it('does not shift the date when the browser sits ahead of UTC', () => {
    expect(formatEnglishLocalDate('2026-01-01')).toBe('Thursday, January 1, 2026');
  });

  it('ignores the browser default locale', () => {
    // The formatter states en-US explicitly, so a Spanish machine still renders English.
    const rendered = formatEnglishLocalDate('2026-07-30') ?? '';

    expect(rendered).not.toContain('jueves');
    expect(rendered).not.toContain('julio');
    expect(rendered).toMatch(/^[A-Za-z]+, [A-Za-z]+ \d{1,2}, \d{4}$/);
  });

  it('returns null for values that are not a calendar date', () => {
    expect(formatEnglishLocalDate(null)).toBeNull();
    expect(formatEnglishLocalDate(undefined)).toBeNull();
    expect(formatEnglishLocalDate('')).toBeNull();
    expect(formatEnglishLocalDate('not-a-date')).toBeNull();
    expect(formatEnglishLocalDate('2026-07-30T13:24:00-06:00')).toBeNull();
  });

  it('returns null for an impossible calendar date instead of rolling it over', () => {
    expect(formatEnglishLocalDate('2026-02-31')).toBeNull();
    expect(formatEnglishLocalDate('2026-13-01')).toBeNull();
  });
});

describe('formatUtcOffset', () => {
  it('labels the zero offset as UTC', () => {
    expect(formatUtcOffset(0)).toBe('UTC');
  });

  it('labels a negative offset', () => {
    expect(formatUtcOffset(-360)).toBe('UTC-06:00');
  });

  it('labels a positive offset', () => {
    expect(formatUtcOffset(540)).toBe('UTC+09:00');
  });

  it('labels an offset that is not a whole hour', () => {
    expect(formatUtcOffset(330)).toBe('UTC+05:30');
    expect(formatUtcOffset(-570)).toBe('UTC-09:30');
  });

  it('falls back to UTC for an unusable value', () => {
    expect(formatUtcOffset(Number.NaN)).toBe('UTC');
  });
});
