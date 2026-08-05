import {
  addDays,
  addMonths,
  buildMonthGrid,
  formatDayLabel,
  formatMinutes,
  formatMonthTitle,
  isIsoLocalDate,
  startOfMonth,
  startOfWeek,
  toInputTime,
  toIsoLocalDate,
  toUtcDate,
  weekDays,
} from './local-date';

describe('local date arithmetic', () => {
  it('rejects a day that does not exist', () => {
    expect(isIsoLocalDate('2026-02-31')).toBe(false);
    expect(isIsoLocalDate('2026-13-01')).toBe(false);
    expect(isIsoLocalDate('30-07-2026')).toBe(false);
    expect(isIsoLocalDate('2026-07-30')).toBe(true);
  });

  it('round-trips a calendar day without shifting it through the browser zone', () => {
    // A local date is a calendar day, not an instant. Building it with Date.UTC and reading it
    // back with the UTC accessors is what keeps 30 July from becoming 29 July west of UTC.
    const date = toUtcDate('2026-07-30');

    expect(date).not.toBeNull();
    expect(toIsoLocalDate(date!)).toBe('2026-07-30');
    expect(date!.getUTCHours()).toBe(0);
  });

  it('adds days across a month boundary', () => {
    expect(addDays('2026-07-31', 1)).toBe('2026-08-01');
    expect(addDays('2026-01-01', -1)).toBe('2025-12-31');
  });

  it('adds days across a leap day', () => {
    expect(addDays('2028-02-28', 1)).toBe('2028-02-29');
    expect(addDays('2026-02-28', 1)).toBe('2026-03-01');
  });

  it('clamps the day of the month when adding months', () => {
    // 31 January plus one month is the end of February, never 2 or 3 March.
    expect(addMonths('2026-01-31', 1)).toBe('2026-02-28');
    expect(addMonths('2028-01-31', 1)).toBe('2028-02-29');
    expect(addMonths('2026-03-31', -1)).toBe('2026-02-28');
  });

  it('finds the first day of a month and the Monday of a week', () => {
    expect(startOfMonth('2026-07-30')).toBe('2026-07-01');
    // 2026-07-30 is a Thursday.
    expect(startOfWeek('2026-07-30')).toBe('2026-07-27');
    // A Sunday belongs to the week that started on the previous Monday.
    expect(startOfWeek('2026-08-02')).toBe('2026-07-27');
  });

  it('lists a week from Monday to Sunday', () => {
    expect(weekDays('2026-07-30')).toEqual([
      '2026-07-27',
      '2026-07-28',
      '2026-07-29',
      '2026-07-30',
      '2026-07-31',
      '2026-08-01',
      '2026-08-02',
    ]);
  });

  it('builds a fixed six-week grid so the calendar height never jumps', () => {
    const grid = buildMonthGrid('2026-07-15');

    expect(grid.length).toBe(42);
    expect(grid[0].date).toBe('2026-06-29');
    expect(grid[0].isCurrentMonth).toBe(false);
    expect(grid.filter((cell) => cell.isCurrentMonth).length).toBe(31);
  });

  it('formats dates in English regardless of the machine language', () => {
    expect(formatMonthTitle('2026-07-30')).toBe('July 2026');
    expect(formatDayLabel('2026-07-30')).toBe('Thursday, July 30');
  });

  it('trims an API time to what a time input expects', () => {
    expect(toInputTime('06:30:00')).toBe('06:30');
    expect(toInputTime(null)).toBe('');
  });

  it('formats minutes as hours and minutes', () => {
    expect(formatMinutes(0)).toBe('0 min');
    expect(formatMinutes(45)).toBe('45 min');
    expect(formatMinutes(60)).toBe('1 h');
    expect(formatMinutes(150)).toBe('2 h 30 min');
  });
});
