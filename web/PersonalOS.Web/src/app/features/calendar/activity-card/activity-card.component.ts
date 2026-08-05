import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  canMarkFailed,
  kindPresentation,
  occurrenceAccessibleName,
  statusIconPath,
  statusLabel,
} from '../../../core/calendar/activity-visuals';
import {
  CalendarOccurrence,
  OccurrenceStatus,
  PLANNING_CATEGORIES,
} from '../../../core/calendar/calendar.models';
import { formatTimeLabel } from '../../../core/time/local-date';

/** What the user asked to do with one occurrence. */
export interface OccurrenceStatusChange {
  readonly occurrence: CalendarOccurrence;
  readonly status: OccurrenceStatus;
}

/**
 * One activity, shown as a semantic button.
 *
 * Everything the card conveys with colour it also conveys with a word: the kind is written on the
 * chip beside its icon, "Important" is a badge rather than a red edge, and a completed or cancelled
 * day says so in text. A user who cannot distinguish the four kind colours therefore loses nothing.
 *
 * Only the actions that make sense are offered. A cancelled day has nothing to complete, and a
 * completed one has nothing to cancel, so those controls are absent rather than present and
 * disabled: a disabled button the user cannot explain is worse than no button.
 */
@Component({
  selector: 'app-activity-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './activity-card.component.html',
  styleUrl: './activity-card.component.scss',
})
export class ActivityCardComponent {
  /** The occurrence being shown. */
  readonly occurrence = input.required<CalendarOccurrence>();

  /** Whether the card offers the complete, edit, and cancel controls. */
  readonly showActions = input(true);

  /** Whether a status change is in flight, so the controls can be disabled. */
  readonly isBusy = input(false);

  /** The account's current local day, which decides whether "Mark failed" makes sense yet. */
  readonly todayDate = input<string | null>(null);

  /** The user asked to edit this activity. */
  readonly open = output<CalendarOccurrence>();

  /** The user asked to record a decision about this day. */
  readonly statusChange = output<OccurrenceStatusChange>();

  protected readonly presentation = computed(() => kindPresentation(this.occurrence().kind));

  protected readonly accessibleName = computed(() => occurrenceAccessibleName(this.occurrence()));

  protected readonly statusText = computed(() => statusLabel(this.occurrence().status));

  protected readonly categoryLabel = computed(
    () =>
      PLANNING_CATEGORIES.find((option) => option.value === this.occurrence().category)?.label
      ?? 'General',
  );

  protected readonly timeLabel = computed(() => {
    const current = this.occurrence();

    if (current.startTime === null) {
      return 'Anytime';
    }

    const start = formatTimeLabel(current.startTime);

    return current.endTime === null ? start : `${start} - ${formatTimeLabel(current.endTime)}`;
  });

  protected readonly statusIcon = computed(() => statusIconPath(this.occurrence().status));

  protected readonly isPlanned = computed(() => this.occurrence().status === 'planned');

  protected readonly isCompleted = computed(() => this.occurrence().status === 'completed');

  protected readonly isFailed = computed(() => this.occurrence().status === 'failed');

  protected readonly isCancelled = computed(() => this.occurrence().status === 'cancelled');

  /** Only a day that is still open, or already ticked off, offers the completion control. */
  protected readonly canComplete = computed(() => this.isPlanned() || this.isCompleted());

  /**
   * Whether "Mark failed" belongs on this card.
   *
   * Only for a day that is still open and has already arrived. A future day has not had its chance
   * yet, so offering the control would invite a claim about something that has not happened.
   */
  protected readonly canMarkFailed = computed(() =>
    canMarkFailed(this.occurrence(), this.todayDate()),
  );

  /** Anything not already called off can be called off. */
  protected readonly canCancel = computed(() => !this.isCancelled());

  /** A recorded outcome can always be undone, whichever way it went. */
  protected readonly canReopen = computed(() => this.isFailed() || this.isCancelled());

  protected onOpen(): void {
    this.open.emit(this.occurrence());
  }

  /** Completing an already completed day reopens it, which is what the control means. */
  protected onToggleCompletion(): void {
    this.statusChange.emit({
      occurrence: this.occurrence(),
      status: this.isCompleted() ? 'planned' : 'completed',
    });
  }

  protected onMarkFailed(): void {
    this.statusChange.emit({ occurrence: this.occurrence(), status: 'failed' });
  }

  protected onCancel(): void {
    this.statusChange.emit({ occurrence: this.occurrence(), status: 'cancelled' });
  }

  protected onReopen(): void {
    this.statusChange.emit({ occurrence: this.occurrence(), status: 'planned' });
  }
}
