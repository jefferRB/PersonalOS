/**
 * Calendar arithmetic for the local dates the server decides.
 *
 * PersonalOS treats a local date as the plain string `yyyy-MM-dd`. It is a calendar day, not an
 * instant, so it must never be converted through the browser time zone: doing that turns
 * `2026-07-30` into 29 July for anyone west of UTC. Every function here therefore builds `Date`
 * objects with `Date.UTC` and reads them back with the `getUTC*` accessors, which makes the
 * browser's own zone irrelevant.
 *
 * The native `Date` and `Intl` APIs cover everything these screens need, so no date library is
 * introduced. A library would add weight and a second set of rules for the same calculations.
 */

/** A local calendar day as `yyyy-MM-dd`. */
export type IsoLocalDate = string;

const ISO_DATE_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

const MONTH_TITLE_FORMATTER = new Intl.DateTimeFormat('en-US', {
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
});

const DAY_LABEL_FORMATTER = new Intl.DateTimeFormat('en-US', {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
  timeZone: 'UTC',
});

const SHORT_DAY_FORMATTER = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  timeZone: 'UTC',
});

/** Weekday headers for a calendar grid, Monday first. */
export const WEEKDAY_HEADERS: readonly { readonly short: string; readonly long: string }[] = [
  { short: 'Mon', long: 'Monday' },
  { short: 'Tue', long: 'Tuesday' },
  { short: 'Wed', long: 'Wednesday' },
  { short: 'Thu', long: 'Thursday' },
  { short: 'Fri', long: 'Friday' },
  { short: 'Sat', long: 'Saturday' },
  { short: 'Sun', long: 'Sunday' },
];

/** Weekday identifiers exactly as the API serializes them. */
export const WEEKDAY_VALUES = [
  'monday',
  'tuesday',
  'wednesday',
  'thursday',
  'friday',
  'saturday',
  'sunday',
] as const;

/** One weekday, as the API spells it. */
export type Weekday = (typeof WEEKDAY_VALUES)[number];

/** Reports whether a value is a well-formed and real calendar day. */
export function isIsoLocalDate(value: unknown): value is IsoLocalDate {
  return typeof value === 'string' && toUtcDate(value) !== null;
}

/**
 * Converts `yyyy-MM-dd` into a `Date` fixed at midnight UTC.
 *
 * @returns `null` when the value is malformed or names a day that does not exist.
 */
export function toUtcDate(value: IsoLocalDate): Date | null {
  const parts = ISO_DATE_PATTERN.exec(value);

  if (parts === null) {
    return null;
  }

  const year = Number(parts[1]);
  const month = Number(parts[2]);
  const day = Number(parts[3]);
  const date = new Date(Date.UTC(year, month - 1, day));

  // Reject values such as 2026-02-31, which JavaScript would silently roll into March.
  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return null;
  }

  return date;
}

/** Converts a `Date` back into `yyyy-MM-dd`, reading its UTC parts. */
export function toIsoLocalDate(date: Date): IsoLocalDate {
  const year = date.getUTCFullYear().toString().padStart(4, '0');
  const month = (date.getUTCMonth() + 1).toString().padStart(2, '0');
  const day = date.getUTCDate().toString().padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/** Moves a calendar day forward or backward by whole days. */
export function addDays(value: IsoLocalDate, days: number): IsoLocalDate {
  const date = toUtcDate(value);

  if (date === null) {
    return value;
  }

  date.setUTCDate(date.getUTCDate() + days);

  return toIsoLocalDate(date);
}

/**
 * Moves a calendar day forward or backward by whole months.
 *
 * The day of the month is clamped, so moving from 31 January lands on 28 or 29 February rather
 * than rolling into March.
 */
export function addMonths(value: IsoLocalDate, months: number): IsoLocalDate {
  const date = toUtcDate(value);

  if (date === null) {
    return value;
  }

  const day = date.getUTCDate();
  const target = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + months, 1));
  const lastDay = new Date(
    Date.UTC(target.getUTCFullYear(), target.getUTCMonth() + 1, 0),
  ).getUTCDate();

  target.setUTCDate(Math.min(day, lastDay));

  return toIsoLocalDate(target);
}

/** The first day of the month a calendar day belongs to. */
export function startOfMonth(value: IsoLocalDate): IsoLocalDate {
  const date = toUtcDate(value);

  if (date === null) {
    return value;
  }

  return toIsoLocalDate(new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1)));
}

/** The Monday of the week a calendar day belongs to. */
export function startOfWeek(value: IsoLocalDate): IsoLocalDate {
  const date = toUtcDate(value);

  if (date === null) {
    return value;
  }

  const offset = (date.getUTCDay() + 6) % 7;

  return addDays(value, -offset);
}

/** The seven days of the week a calendar day belongs to, Monday first. */
export function weekDays(value: IsoLocalDate): IsoLocalDate[] {
  const monday = startOfWeek(value);

  return Array.from({ length: 7 }, (_, index) => addDays(monday, index));
}

/** One cell of a month grid. */
export interface CalendarCell {
  readonly date: IsoLocalDate;
  /** Day number shown in the cell. */
  readonly dayOfMonth: number;
  /** Whether the cell belongs to the month being displayed. */
  readonly isCurrentMonth: boolean;
}

/**
 * One position in a month grid: either a real day of the visible month, or blank space.
 *
 * A blank is not a date. It carries no day number and nothing to click, because a grid that shows
 * the 29th of the previous month invites the user to act on a month they are not looking at.
 */
export type MonthGridCell =
  | { readonly kind: 'day'; readonly date: IsoLocalDate; readonly dayOfMonth: number }
  | { readonly kind: 'placeholder'; readonly key: string };

/**
 * Builds the six-week grid a month view renders.
 *
 * A fixed six-week grid keeps the calendar the same height every month, so navigating between
 * months does not make the page jump.
 */
export function buildMonthGrid(monthAnchor: IsoLocalDate): CalendarCell[] {
  const first = startOfMonth(monthAnchor);
  const firstDate = toUtcDate(first);

  if (firstDate === null) {
    return [];
  }

  const month = firstDate.getUTCMonth();
  const gridStart = startOfWeek(first);

  return Array.from({ length: 42 }, (_, index) => {
    const date = addDays(gridStart, index);
    const parsed = toUtcDate(date)!;

    return {
      date,
      dayOfMonth: parsed.getUTCDate(),
      isCurrentMonth: parsed.getUTCMonth() === month,
    };
  });
}

/**
 * Builds a month grid holding only the days of the visible month.
 *
 * The leading and trailing positions are blanks rather than the neighbouring months' dates. Showing
 * those dates makes a month look like it starts on the 29th and invites clicks that jump the user
 * somewhere they did not ask to go; blanks keep the weekday columns aligned without pretending to
 * be days.
 *
 * The grid is padded to whole weeks so the columns stay square, and no further: a five-week month
 * renders five rows rather than a wasted sixth.
 */
export function buildMonthCells(monthAnchor: IsoLocalDate): MonthGridCell[] {
  const first = startOfMonth(monthAnchor);
  const firstDate = toUtcDate(first);

  if (firstDate === null) {
    return [];
  }

  const year = firstDate.getUTCFullYear();
  const month = firstDate.getUTCMonth();
  const daysInMonth = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
  // Monday-first, so Monday contributes no leading blanks and Sunday contributes six.
  const leading = ((firstDate.getUTCDay() + 6) % 7);

  const cells: MonthGridCell[] = [];

  for (let index = 0; index < leading; index += 1) {
    cells.push({ kind: 'placeholder', key: `lead-${index}` });
  }

  for (let day = 0; day < daysInMonth; day += 1) {
    const date = addDays(first, day);

    cells.push({ kind: 'day', date, dayOfMonth: day + 1 });
  }

  const trailing = (7 - (cells.length % 7)) % 7;

  for (let index = 0; index < trailing; index += 1) {
    cells.push({ kind: 'placeholder', key: `trail-${index}` });
  }

  return cells;
}

/** The last day of the month a calendar day belongs to. */
export function endOfMonth(value: IsoLocalDate): IsoLocalDate {
  return addDays(addMonths(startOfMonth(value), 1), -1);
}

/**
 * Moves within a month without ever leaving it.
 *
 * Arrow keys step over the blanks because a blank is not somewhere the user can be. Reaching the
 * edge of the month stops there rather than silently loading another one, which would move the grid
 * out from under the person pressing the key.
 */
export function clampToMonth(value: IsoLocalDate, monthAnchor: IsoLocalDate): IsoLocalDate {
  const first = startOfMonth(monthAnchor);
  const last = endOfMonth(monthAnchor);

  if (value < first) {
    return first;
  }

  return value > last ? last : value;
}

/** Formats a calendar day as `July 2026`. */
export function formatMonthTitle(value: IsoLocalDate): string {
  const date = toUtcDate(value);

  return date === null ? '' : MONTH_TITLE_FORMATTER.format(date);
}

/** Formats a calendar day as `Thursday, July 30`. */
export function formatDayLabel(value: IsoLocalDate): string {
  const date = toUtcDate(value);

  return date === null ? '' : DAY_LABEL_FORMATTER.format(date);
}

/**
 * Names a day relative to the account's current day, when there is a word for it.
 *
 * @returns `Today`, `Tomorrow`, `Yesterday`, or `null` when the day needs its date spelled out.
 * The reference day comes from the server, never from the browser clock, so a user whose device is
 * in another zone still reads the right word.
 */
export function relativeDayName(
  value: IsoLocalDate,
  todayLocalDate: IsoLocalDate | null,
): string | null {
  if (todayLocalDate === null) {
    return null;
  }

  const target = toUtcDate(value);
  const today = toUtcDate(todayLocalDate);

  if (target === null || today === null) {
    return null;
  }

  const offsetDays = Math.round((target.getTime() - today.getTime()) / 86_400_000);

  switch (offsetDays) {
    case 0:
      return 'Today';
    case 1:
      return 'Tomorrow';
    case -1:
      return 'Yesterday';
    default:
      return null;
  }
}

/** Formats a calendar day as `Jul 30`. */
export function formatShortDate(value: IsoLocalDate): string {
  const date = toUtcDate(value);

  return date === null ? '' : SHORT_DAY_FORMATTER.format(date);
}

/**
 * Trims an API time value down to what an `<input type="time">` expects.
 *
 * The API sends `HH:mm:ss`; the control accepts `HH:mm`. Sending the longer form back is
 * harmless, so only the display direction needs converting.
 */
export function toInputTime(value: string | null | undefined): string {
  return typeof value === 'string' && value.length >= 5 ? value.slice(0, 5) : '';
}

/**
 * Formats an API time value for display, as `06:30`.
 *
 * A 24-hour clock is used because it is unambiguous and needs no locale-specific meridiem.
 */
export function formatTimeLabel(value: string | null | undefined): string {
  return toInputTime(value);
}

/** How many minutes one timeline slot covers. */
export const SLOT_MINUTES = 15;

/** How many slots a full day holds at {@link SLOT_MINUTES} resolution. */
export const SLOTS_PER_DAY = (24 * 60) / SLOT_MINUTES;

/**
 * Converts an API time value into minutes since midnight.
 *
 * @returns `null` when the value is absent or malformed, so a caller never places a block at
 * midnight because a field happened to be empty.
 */
export function toMinutesOfDay(value: string | null | undefined): number | null {
  if (typeof value !== 'string') {
    return null;
  }

  const parts = /^(\d{2}):(\d{2})/.exec(value);

  if (parts === null) {
    return null;
  }

  const hours = Number(parts[1]);
  const minutes = Number(parts[2]);

  if (hours > 23 || minutes > 59) {
    return null;
  }

  return hours * 60 + minutes;
}

/** Converts minutes since midnight into the `HH:mm` an `<input type="time">` expects. */
export function fromMinutesOfDay(totalMinutes: number): string {
  const clamped = Math.max(0, Math.min(24 * 60 - 1, Math.round(totalMinutes)));
  const hours = Math.floor(clamped / 60)
    .toString()
    .padStart(2, '0');
  const minutes = (clamped % 60).toString().padStart(2, '0');

  return `${hours}:${minutes}`;
}

/** Formats a whole number of minutes as `1 h 25 min`. */
export function formatMinutes(totalMinutes: number): string {
  if (!Number.isFinite(totalMinutes) || totalMinutes <= 0) {
    return '0 min';
  }

  const minutes = Math.round(totalMinutes);
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;

  if (hours === 0) {
    return `${rest} min`;
  }

  return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`;
}
