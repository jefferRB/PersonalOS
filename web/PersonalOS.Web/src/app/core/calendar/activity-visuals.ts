import { CalendarOccurrence, OccurrenceStatus, PlanningItemKind } from './calendar.models';

/**
 * How each kind of calendar item is presented.
 *
 * The kind decides the colour of a block, not the category. There are four kinds and seven
 * categories, and a user reading a week needs to know "is this a commitment to somebody else or a
 * task I can move" far more often than they need to know which area of life it belongs to. The
 * category is still shown, as text on the card.
 *
 * Every entry carries a label and an icon as well as a token, because colour is never allowed to be
 * the only thing that distinguishes two kinds.
 */
export interface KindPresentation {
  readonly kind: PlanningItemKind;
  /** Word shown on the card and read out by assistive technology. */
  readonly label: string;
  /** Suffix of the `--kind-*` custom properties this kind uses. */
  readonly token: PlanningItemKind;
  /** `d` attribute of a 24×24 icon path, so no icon package is needed. */
  readonly iconPath: string;
}

const KIND_PRESENTATIONS: Readonly<Record<PlanningItemKind, KindPresentation>> = {
  task: {
    kind: 'task',
    label: 'Task',
    token: 'task',
    iconPath: 'M4 12l5 5 11-11',
  },
  routine: {
    kind: 'routine',
    label: 'Routine',
    token: 'routine',
    iconPath: 'M20 12a8 8 0 1 1-2.3-5.6M20 3v4h-4',
  },
  event: {
    kind: 'event',
    label: 'Event',
    token: 'event',
    iconPath: 'M4 6h16v14H4zM4 10h16M8 3v4M16 3v4',
  },
  appointment: {
    kind: 'appointment',
    label: 'Appointment',
    token: 'appointment',
    iconPath: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18ZM12 7v5l3 2',
  },
};

const STATUS_LABELS: Readonly<Record<OccurrenceStatus, string>> = {
  planned: 'Planned',
  completed: 'Completed',
  failed: 'Failed',
  cancelled: 'Cancelled',
};

/**
 * A glyph for each recorded outcome.
 *
 * Failed and cancelled must never look alike: one is a commitment the user did not keep, the other
 * one they deliberately let go. Different marks say that where a shade of grey would not.
 */
const STATUS_ICON_PATHS: Readonly<Record<OccurrenceStatus, string>> = {
  planned: 'M12 7v5l3 2M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z',
  completed: 'M5 13l4 4L19 7',
  failed: 'M12 8v5M12 16v.5M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z',
  cancelled: 'M6 6l12 12M18 6L6 18',
};

/** How one kind should be presented. */
export function kindPresentation(kind: PlanningItemKind): KindPresentation {
  return KIND_PRESENTATIONS[kind] ?? KIND_PRESENTATIONS.task;
}

/** Every kind presentation, in the order the legend lists them. */
export const KIND_PRESENTATIONS_IN_ORDER: readonly KindPresentation[] = [
  KIND_PRESENTATIONS.task,
  KIND_PRESENTATIONS.routine,
  KIND_PRESENTATIONS.event,
  KIND_PRESENTATIONS.appointment,
];

/** The word describing an occurrence's state, so meaning never depends on colour alone. */
export function statusLabel(status: OccurrenceStatus): string {
  return STATUS_LABELS[status] ?? STATUS_LABELS.planned;
}

/** The glyph for an occurrence's state, paired with its word rather than replacing it. */
export function statusIconPath(status: OccurrenceStatus): string {
  return STATUS_ICON_PATHS[status] ?? STATUS_ICON_PATHS.planned;
}

/**
 * Whether an occurrence can still be marked failed.
 *
 * Only a day that has already arrived can have been missed, and only a day nobody has decided about
 * yet is worth deciding. The reference day comes from the server, so a device in another time zone
 * cannot offer the control a day early.
 */
export function canMarkFailed(
  occurrence: CalendarOccurrence,
  todayLocalDate: string | null,
): boolean {
  return (
    occurrence.status === 'planned'
    && todayLocalDate !== null
    && occurrence.occurrenceDate <= todayLocalDate
  );
}

/**
 * Builds the accessible name of an activity.
 *
 * It states the kind, the title, the time, and the state in words, because a screen reader user
 * receives none of the colour, the icon, or the position in the timeline that a sighted user does.
 */
export function occurrenceAccessibleName(occurrence: CalendarOccurrence): string {
  const parts: string[] = [kindPresentation(occurrence.kind).label, occurrence.title];

  if (occurrence.startTime !== null) {
    parts.push(
      occurrence.endTime === null
        ? `at ${occurrence.startTime.slice(0, 5)}`
        : `from ${occurrence.startTime.slice(0, 5)} to ${occurrence.endTime.slice(0, 5)}`,
    );
  } else {
    parts.push('anytime');
  }

  if (occurrence.priority === 'high') {
    parts.push('important');
  }

  if (occurrence.status !== 'planned') {
    parts.push(statusLabel(occurrence.status).toLowerCase());
  }

  return parts.join(', ');
}
