import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  kindPresentation,
  occurrenceAccessibleName,
  statusLabel,
} from '../../../core/calendar/activity-visuals';
import { CalendarOccurrence } from '../../../core/calendar/calendar.models';
import { TimelineWindow, layOutDay, nowRow } from '../../../core/calendar/timeline-layout';
import { formatTimeLabel } from '../../../core/time/local-date';

/**
 * The day's timeline, drawn at the account's configured hours and resolution.
 *
 * Slots are real buttons rather than clickable divs, so the whole day is reachable with a keyboard
 * and every slot announces the time it would create. Activities are laid out over the same grid and
 * overlapping ones are put side by side, because a calendar that hides the second of two clashing
 * commitments hides the exact thing the user opened it to find.
 *
 * There is no drag, no drop, and no resize. Times are changed by typing them in the editor, which
 * works with any pointer, any motor precision, and costs no dependency.
 */
@Component({
  selector: 'app-day-timeline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './day-timeline.component.html',
  styleUrl: './day-timeline.component.scss',
})
export class DayTimelineComponent {
  readonly occurrences = input.required<readonly CalendarOccurrence[]>();

  /** The visible hours and resolution the account chose. */
  readonly window = input.required<TimelineWindow>();

  /**
   * The account's current local time as `HH:mm:ss`, or `null` when the day being shown is not
   * today. The marker is never drawn from the browser clock.
   */
  readonly localTimeOfDay = input<string | null>(null);

  /** The user clicked an empty slot and wants to create something at that time. */
  readonly slotSelected = output<string>();

  /** The user clicked an activity and wants to edit it. */
  readonly occurrenceSelected = output<CalendarOccurrence>();

  protected readonly layout = computed(() => layOutDay(this.occurrences(), this.window()));

  protected readonly slots = computed(() => this.layout().slots);

  /** Where the "now" line belongs, as a one-based grid row, or `null` on any other day. */
  protected readonly nowRow = computed(() => nowRow(this.localTimeOfDay(), this.window()));

  protected accessibleName(occurrence: CalendarOccurrence): string {
    return occurrenceAccessibleName(occurrence);
  }

  protected iconPath(occurrence: CalendarOccurrence): string {
    return kindPresentation(occurrence.kind).iconPath;
  }

  protected kindToken(occurrence: CalendarOccurrence): string {
    return kindPresentation(occurrence.kind).token;
  }

  protected kindLabel(occurrence: CalendarOccurrence): string {
    return kindPresentation(occurrence.kind).label;
  }

  protected status(occurrence: CalendarOccurrence): string {
    return statusLabel(occurrence.status);
  }

  protected timeLabel(occurrence: CalendarOccurrence): string {
    return formatTimeLabel(occurrence.startTime);
  }

  protected slotLabel(time: string): string {
    return `Add an activity at ${time}`;
  }
}
