import { CalendarOccurrence, PlanningItemKind } from './calendar.models';

/** Which kinds a section is showing. */
export type KindFilter = 'all' | PlanningItemKind;

/**
 * Which occurrences a section is showing, by what the user decided about them.
 *
 * `important` is the one hybrid: it means "still going ahead, and worth knowing about", which is
 * how the agenda's picker offers it. The seven-day section expresses the same idea with its own
 * `importantOnly` toggle instead, so that it can combine importance with a status view.
 */
export type ViewFilter =
  | 'open'
  | 'all'
  | 'important'
  | 'completed'
  | 'failed'
  | 'cancelled';

/** What a section is currently showing. */
export interface OccurrenceFilter {
  readonly kind: KindFilter;
  readonly view: ViewFilter;
  /** Restricts to occurrences the server marked important. Used by the seven-day section. */
  readonly importantOnly: boolean;
}

/** One option in a filter picker. */
export interface FilterOption<TValue> {
  readonly value: TValue;
  readonly label: string;
}

/** Kind options, shared by both sections. */
export const KIND_FILTER_OPTIONS: readonly FilterOption<KindFilter>[] = [
  { value: 'all', label: 'All kinds' },
  { value: 'task', label: 'Task' },
  { value: 'routine', label: 'Routine' },
  { value: 'event', label: 'Event' },
  { value: 'appointment', label: 'Appointment' },
];

/** View options offered by the daily agenda. */
export const DAY_VIEW_OPTIONS: readonly FilterOption<ViewFilter>[] = [
  { value: 'open', label: 'Open' },
  { value: 'all', label: 'All' },
  { value: 'important', label: 'Important' },
  { value: 'completed', label: 'Completed' },
  { value: 'failed', label: 'Failed' },
  { value: 'cancelled', label: 'Cancelled' },
];

/**
 * View options offered by the seven-day section.
 *
 * There is no cancelled view here: the section answers "what is coming", and a day the user already
 * called off is not coming. Importance is a separate toggle so it can combine with either view.
 */
export const UPCOMING_VIEW_OPTIONS: readonly FilterOption<ViewFilter>[] = [
  { value: 'open', label: 'Open' },
  { value: 'all', label: 'All' },
  { value: 'completed', label: 'Completed' },
  { value: 'failed', label: 'Failed' },
];

/** What the daily agenda shows before the user touches anything. */
export const DEFAULT_DAY_FILTER: OccurrenceFilter = {
  kind: 'all',
  view: 'open',
  importantOnly: false,
};

/**
 * What the seven-day section shows before the user touches anything.
 *
 * Important-only is on and the view is open, which together reproduce exactly what the section
 * showed when the server did the filtering: events and appointments, high-priority tasks and
 * routines, and nothing that was completed or called off.
 */
export const DEFAULT_UPCOMING_FILTER: OccurrenceFilter = {
  kind: 'all',
  view: 'open',
  importantOnly: true,
};

/** Whether a filter still matches its defaults, so a Clear control can hide itself. */
export function isDefaultFilter(
  filter: OccurrenceFilter,
  defaults: OccurrenceFilter,
): boolean {
  return (
    filter.kind === defaults.kind
    && filter.view === defaults.view
    && filter.importantOnly === defaults.importantOnly
  );
}

/** Whether one occurrence survives a filter. */
export function matchesFilter(
  occurrence: CalendarOccurrence,
  filter: OccurrenceFilter,
): boolean {
  if (filter.kind !== 'all' && occurrence.kind !== filter.kind) {
    return false;
  }

  if (filter.importantOnly && !occurrence.isImportant) {
    return false;
  }

  switch (filter.view) {
    case 'open':
      return occurrence.status === 'planned';
    case 'completed':
      return occurrence.status === 'completed';
    case 'failed':
      return occurrence.status === 'failed';
    case 'cancelled':
      return occurrence.status === 'cancelled';
    case 'important':
      // Something already dealt with is no longer worth flagging, whichever way it went.
      return occurrence.isImportant && occurrence.status === 'planned';
    case 'all':
      return true;
  }
}

/**
 * Applies a filter to a list.
 *
 * The result is always a new array. Filtering must never reorder or edit what the store holds,
 * because two sections read the same day and a mutation in one would surface in the other.
 */
export function filterOccurrences(
  occurrences: readonly CalendarOccurrence[],
  filter: OccurrenceFilter,
): CalendarOccurrence[] {
  return occurrences.filter((occurrence) => matchesFilter(occurrence, filter));
}

/**
 * Splits a day into its untimed and timed activities, each in display order.
 *
 * Untimed activities come first wherever a day is shown, so the split is done once here rather than
 * repeated by every screen that needs it.
 */
export function splitByTime(occurrences: readonly CalendarOccurrence[]): {
  readonly anytime: CalendarOccurrence[];
  readonly scheduled: CalendarOccurrence[];
} {
  return {
    anytime: occurrences.filter((occurrence) => occurrence.startTime === null),
    scheduled: occurrences.filter((occurrence) => occurrence.startTime !== null),
  };
}
