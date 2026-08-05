import { IsoLocalDate, Weekday } from '../time/local-date';

/** What sort of thing a calendar item is, exactly as the API spells it. */
export type PlanningItemKind = 'task' | 'routine' | 'event' | 'appointment';

/** Which area of life an item belongs to, exactly as the API spells it. */
export type PlanningCategory =
  | 'general'
  | 'personal'
  | 'work'
  | 'study'
  | 'health'
  | 'fitness'
  | 'nutrition';

/** How much an item matters, exactly as the API spells it. */
export type PlanningPriority = 'low' | 'normal' | 'high';

/** What the user decided about one occurrence, exactly as the API spells it. */
export type OccurrenceStatus = 'planned' | 'completed' | 'failed' | 'cancelled';

/** How often an item repeats, exactly as the API spells it. */
export type RecurrenceFrequency = 'none' | 'daily' | 'weekly' | 'monthly';

/** Options offered by the kind picker. */
export const PLANNING_KINDS: readonly { value: PlanningItemKind; label: string }[] = [
  { value: 'task', label: 'Task' },
  { value: 'routine', label: 'Routine' },
  { value: 'event', label: 'Event' },
  { value: 'appointment', label: 'Appointment' },
];

/** Options offered by the category picker. */
export const PLANNING_CATEGORIES: readonly { value: PlanningCategory; label: string }[] = [
  { value: 'general', label: 'General' },
  { value: 'personal', label: 'Personal' },
  { value: 'work', label: 'Work' },
  { value: 'study', label: 'Study' },
  { value: 'health', label: 'Health' },
  { value: 'fitness', label: 'Fitness' },
  { value: 'nutrition', label: 'Nutrition' },
];

/** Options offered by the priority picker. */
export const PLANNING_PRIORITIES: readonly { value: PlanningPriority; label: string }[] = [
  { value: 'low', label: 'Low' },
  { value: 'normal', label: 'Normal' },
  { value: 'high', label: 'High' },
];

/** Options offered by the repetition picker. */
export const RECURRENCE_FREQUENCIES: readonly {
  value: RecurrenceFrequency;
  label: string;
}[] = [
  { value: 'none', label: 'Does not repeat' },
  { value: 'daily', label: 'Every day' },
  { value: 'weekly', label: 'Every week' },
  { value: 'monthly', label: 'Every month' },
];

/** A recurrence rule, as returned by the calendar endpoints. */
export interface Recurrence {
  readonly frequency: RecurrenceFrequency;
  readonly interval: number;
  readonly endDate: IsoLocalDate | null;
  readonly selectedWeekdays: readonly Weekday[];
}

/** One calendar item with its rule, as `GET /api/calendar/items/{id}` returns it. */
export interface PlanningItem {
  readonly id: string;
  readonly title: string;
  readonly description: string | null;
  readonly kind: PlanningItemKind;
  readonly category: PlanningCategory;
  readonly priority: PlanningPriority;
  readonly startDate: IsoLocalDate;
  /** Local start time as `HH:mm:ss`, or `null` for an item with no time. */
  readonly startTime: string | null;
  readonly endTime: string | null;
  readonly recurrence: Recurrence;
  /**
   * Whether the repetition can still be changed.
   *
   * The server freezes it once a day has been completed or cancelled, so the editor disables those
   * controls rather than letting the user fill in a form that cannot be saved.
   */
  readonly isRecurrencePatternLocked: boolean;
}

/** One calendar item on one local calendar day. */
export interface CalendarOccurrence {
  readonly planningItemId: string;
  readonly occurrenceDate: IsoLocalDate;
  readonly title: string;
  readonly description: string | null;
  readonly kind: PlanningItemKind;
  readonly category: PlanningCategory;
  readonly priority: PlanningPriority;
  readonly startTime: string | null;
  readonly endTime: string | null;
  readonly status: OccurrenceStatus;
  readonly isRecurring: boolean;
  /**
   * Whether this is something the user should not be surprised by.
   *
   * The server decides: events and appointments always are, a task or a routine only when marked
   * high priority. Filtering on the server's answer keeps one definition of the rule.
   */
  readonly isImportant: boolean;
  readonly completedAtUtc: string | null;
}

/** How many of one kind fall on a day. */
export interface DayKindCount {
  readonly kind: PlanningItemKind;
  readonly count: number;
}

/** What one cell of the month grid needs, with no private text in it. */
export interface CalendarDaySummary {
  readonly date: IsoLocalDate;
  readonly totalCount: number;
  readonly completedCount: number;
  /** How many were expected and did not happen. */
  readonly failedCount: number;
  readonly cancelledCount: number;
  /** Which kinds appear on the day and how many of each, busiest first. */
  readonly kinds: readonly DayKindCount[];
  readonly hasHighPriority: boolean;
}

/** One month of the calendar grid, as `GET /api/calendar/month` returns it. */
export interface CalendarMonth {
  readonly year: number;
  readonly month: number;
  readonly fromDate: IsoLocalDate;
  readonly toDate: IsoLocalDate;
  readonly todayLocalDate: IsoLocalDate;
  readonly timeZoneId: string;
  readonly days: readonly CalendarDaySummary[];
}

/** One local calendar day, as `GET /api/calendar/day` returns it. */
export interface CalendarDay {
  readonly date: IsoLocalDate;
  readonly todayLocalDate: IsoLocalDate;
  readonly timeZoneId: string;
  /** The account's current local time as `HH:mm:ss`, decided by the server. */
  readonly localTimeOfDay: string;
  readonly occurrences: readonly CalendarOccurrence[];
}

/** The occurrences of one day inside the upcoming window. */
export interface UpcomingDay {
  readonly date: IsoLocalDate;
  readonly occurrences: readonly CalendarOccurrence[];
}

/**
 * The next seven local days, as `GET /api/calendar/upcoming` returns them.
 *
 * Everything in the window arrives, not only the important entries, which is what lets the
 * section's filters run without a request per click.
 */
export interface UpcomingWeek {
  readonly fromDate: IsoLocalDate;
  readonly toDate: IsoLocalDate;
  readonly todayLocalDate: IsoLocalDate;
  readonly timeZoneId: string;
  readonly days: readonly UpcomingDay[];
}

/** Values sent for a recurrence rule. */
export interface SaveRecurrenceRequest {
  readonly frequency: RecurrenceFrequency;
  readonly interval: number;
  readonly endDate: IsoLocalDate | null;
  readonly selectedWeekdays: readonly Weekday[];
}

/** Values sent when creating or editing a calendar item. */
export interface SavePlanningItemRequest {
  readonly title: string;
  readonly description: string | null;
  readonly kind: PlanningItemKind;
  readonly category: PlanningCategory;
  readonly priority: PlanningPriority;
  readonly startDate: IsoLocalDate;
  readonly startTime: string | null;
  readonly endTime: string | null;
  readonly recurrence: SaveRecurrenceRequest;
}
