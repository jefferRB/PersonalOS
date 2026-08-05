import { fromMinutesOfDay, toMinutesOfDay } from '../time/local-date';
import { CalendarOccurrence } from './calendar.models';

/** The visible window and resolution the planner's timeline is drawn at. */
export interface TimelineWindow {
  /** First visible minute of the day. */
  readonly startMinutes: number;
  /** Last visible minute of the day. The final slot ends here. */
  readonly endMinutes: number;
  /** How many minutes each slot covers. */
  readonly intervalMinutes: number;
}

/** One clickable slot on the timeline. */
export interface TimelineSlot {
  /** Zero-based position inside the visible window. */
  readonly index: number;
  /** `HH:mm`, used as the label and as the value handed to the editor. */
  readonly time: string;
  /** Whether this slot starts a whole hour, which is where the gutter draws its labels. */
  readonly isHourStart: boolean;
}

/** One timed activity, placed on the timeline grid. */
export interface TimelineBlock {
  readonly occurrence: CalendarOccurrence;
  /** One-based CSS grid row the block starts on. */
  readonly rowStart: number;
  /** How many rows the block covers. Never less than one. */
  readonly rowSpan: number;
  /** One-based CSS grid column, so two overlapping activities sit side by side. */
  readonly column: number;
}

/** The result of laying out one day. */
export interface TimelineLayout {
  readonly slots: readonly TimelineSlot[];
  readonly blocks: readonly TimelineBlock[];
  /** How many columns the grid needs, which is the widest overlap of the day. */
  readonly columnCount: number;
  /**
   * Timed activities that fall entirely outside the visible window.
   *
   * They are handed back rather than dropped so the planner can offer them in its own section. An
   * activity that vanished because of a display setting would be the worst possible outcome of
   * choosing one.
   */
  readonly outsideWindow: readonly CalendarOccurrence[];
}

/** The shortest a block may be drawn, whatever the configured interval. */
const MINIMUM_BLOCK_MINUTES = 30;

/** Builds the clickable slots of a visible window. */
export function buildSlots(window: TimelineWindow): TimelineSlot[] {
  const { startMinutes, endMinutes, intervalMinutes } = window;

  if (intervalMinutes <= 0 || endMinutes <= startMinutes) {
    return [];
  }

  const count = Math.ceil((endMinutes - startMinutes) / intervalMinutes);

  return Array.from({ length: count }, (_, index) => {
    const minutes = startMinutes + index * intervalMinutes;

    return {
      index,
      time: fromMinutesOfDay(minutes),
      isHourStart: minutes % 60 === 0,
    };
  });
}

/**
 * Places the timed activities of one day onto the timeline grid.
 *
 * Overlapping activities are put in separate columns rather than stacked on top of each other. A
 * calendar that hides the second of two overlapping commitments is worse than useless, because the
 * user cannot see the clash they most need to know about. Columns are assigned greedily: each
 * activity takes the first column whose previous activity has already finished, which is the
 * standard interval-partitioning result and uses the fewest columns possible.
 *
 * An activity that starts before the visible window but runs into it is clipped to the window and
 * still drawn. Only one that never overlaps the window at all is handed back separately.
 *
 * The function is pure, so the layout can be tested without rendering anything.
 */
export function layOutDay(
  occurrences: readonly CalendarOccurrence[],
  window: TimelineWindow,
): TimelineLayout {
  const slots = buildSlots(window);
  const { startMinutes, endMinutes, intervalMinutes } = window;

  if (slots.length === 0) {
    return { slots, blocks: [], columnCount: 1, outsideWindow: [] };
  }

  const timed = occurrences
    .map((occurrence) => ({ occurrence, start: toMinutesOfDay(occurrence.startTime) }))
    .filter(
      (candidate): candidate is { occurrence: CalendarOccurrence; start: number } =>
        candidate.start !== null,
    )
    .sort((left, right) => left.start - right.start);

  const columnEnds: number[] = [];
  const blocks: TimelineBlock[] = [];
  const outsideWindow: CalendarOccurrence[] = [];

  for (const { occurrence, start } of timed) {
    const declaredEnd = toMinutesOfDay(occurrence.endTime) ?? start + intervalMinutes;
    // A block shorter than half an hour is too small to hold a title and too small to click
    // reliably, so it is drawn at a floor even when the real activity is briefer.
    const end = Math.max(declaredEnd, start + MINIMUM_BLOCK_MINUTES);

    if (end <= startMinutes || start >= endMinutes) {
      outsideWindow.push(occurrence);

      continue;
    }

    const clippedStart = Math.max(start, startMinutes);
    const clippedEnd = Math.min(end, endMinutes);

    let column = columnEnds.findIndex((columnEnd) => columnEnd <= start);

    if (column === -1) {
      column = columnEnds.length;
    }

    columnEnds[column] = end;

    const rowStart = Math.floor((clippedStart - startMinutes) / intervalMinutes);
    const rowEnd = Math.min(
      slots.length,
      Math.ceil((clippedEnd - startMinutes) / intervalMinutes),
    );

    blocks.push({
      occurrence,
      rowStart: rowStart + 1,
      rowSpan: Math.max(1, rowEnd - rowStart),
      column: column + 1,
    });
  }

  return {
    slots,
    blocks,
    columnCount: Math.max(1, columnEnds.length),
    outsideWindow,
  };
}

/**
 * Where the "now" line belongs, as a one-based grid row.
 *
 * @returns `null` when the current time is outside the visible window, or when the day being shown
 * is not the account's current day. Drawing a "now" marker on another date would be a lie.
 */
export function nowRow(localTimeOfDay: string | null, window: TimelineWindow): number | null {
  const minutes = toMinutesOfDay(localTimeOfDay);

  if (
    minutes === null
    || window.intervalMinutes <= 0
    || minutes < window.startMinutes
    || minutes >= window.endMinutes
  ) {
    return null;
  }

  return Math.floor((minutes - window.startMinutes) / window.intervalMinutes) + 1;
}
