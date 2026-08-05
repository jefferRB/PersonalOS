/**
 * How the day planner's timeline is shown to the authenticated account.
 *
 * These are display choices, not rules about when activities may happen. An activity outside the
 * visible window still exists and the planner still offers a way to reach it.
 */
export interface CalendarDisplay {
  /** First visible local time as `HH:mm:ss`. */
  readonly dayStartTime: string;
  /** Last visible local time as `HH:mm:ss`. */
  readonly dayEndTime: string;
  readonly slotMinutes: number;
}

/** Interval lengths the planner offers. */
export const SLOT_MINUTE_OPTIONS: readonly number[] = [15, 30, 60];

/** What an account that has never chosen sees. */
export const DEFAULT_CALENDAR_DISPLAY: CalendarDisplay = {
  dayStartTime: '06:00:00',
  dayEndTime: '22:00:00',
  slotMinutes: 15,
};

/** Profile of the authenticated account, as returned by `GET /api/profile`. */
export interface UserProfile {
  readonly displayName: string;
  /** Sign-in address. Read-only in Milestone 2. */
  readonly email: string;
  /** Persisted IANA time-zone identifier, for example `America/Costa_Rica`. */
  readonly timeZoneId: string;
  readonly calendarDisplay: CalendarDisplay;
  readonly updatedAtUtc: string;
}

/** Values a client may change through `PUT /api/profile`. */
export interface UpdateProfileRequest {
  readonly displayName: string;
  readonly timeZoneId: string;
}

/** Values a client may change through `PUT /api/profile/calendar-display`. */
export interface UpdateCalendarDisplayRequest {
  readonly dayStartTime: string;
  readonly dayEndTime: string;
  readonly slotMinutes: number;
}

/** Current instant expressed for the authenticated account, from `GET /api/time/context`. */
export interface TimeContext {
  readonly utcNow: string;
  readonly localNow: string;
  /** The account's local calendar date as `yyyy-MM-dd`, decided by the server. */
  readonly localDate: string;
  readonly timeZoneId: string;
  readonly utcOffsetMinutes: number;
}
