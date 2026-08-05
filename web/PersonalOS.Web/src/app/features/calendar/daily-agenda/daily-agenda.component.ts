import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { CalendarOccurrence } from '../../../core/calendar/calendar.models';
import {
  DAY_VIEW_OPTIONS,
  OccurrenceFilter,
} from '../../../core/calendar/occurrence-filters';
import { IsoLocalDate, formatDayLabel, relativeDayName } from '../../../core/time/local-date';
import {
  ActivityCardComponent,
  OccurrenceStatusChange,
} from '../activity-card/activity-card.component';
import { OccurrenceFiltersComponent } from '../occurrence-filters/occurrence-filters.component';

/**
 * The agenda for one local calendar day, beside the month grid.
 *
 * The title names the day the way a person would: "Today's agenda" rather than a date the user has
 * to decode. The date itself stays underneath, because "Today" alone is ambiguous once the page has
 * been open across midnight.
 *
 * Filtering happens on data the page already has. A day is bounded, so changing a filter is
 * arithmetic rather than a round trip, and the list the store holds is never edited.
 */
@Component({
  selector: 'app-daily-agenda',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ActivityCardComponent, OccurrenceFiltersComponent],
  templateUrl: './daily-agenda.component.html',
  styleUrl: './daily-agenda.component.scss',
})
export class DailyAgendaComponent {
  readonly date = input.required<IsoLocalDate>();

  /** The account's current local day, as decided by the server. */
  readonly todayDate = input<IsoLocalDate | null>(null);

  readonly anytime = input.required<readonly CalendarOccurrence[]>();

  readonly scheduled = input.required<readonly CalendarOccurrence[]>();

  readonly filter = input.required<OccurrenceFilter>();

  readonly isFilterDefault = input(true);

  readonly hiddenCount = input(0);

  readonly isLoading = input(false);

  readonly error = input<string | null>(null);

  readonly busyItemId = input<string | null>(null);

  /** The user moved the agenda by whole days. */
  readonly dayOffset = output<number>();

  readonly goToToday = output<void>();

  /** The user asked to open the planner for this day. */
  readonly planDay = output<void>();

  readonly open = output<CalendarOccurrence>();

  readonly statusChange = output<OccurrenceStatusChange>();

  readonly filterChange = output<OccurrenceFilter>();

  readonly filterCleared = output<void>();

  readonly retry = output<void>();

  protected readonly viewOptions = DAY_VIEW_OPTIONS;

  protected readonly dateLabel = computed(() => formatDayLabel(this.date()));

  protected readonly isToday = computed(
    () => this.todayDate() !== null && this.date() === this.todayDate(),
  );

  /**
   * The heading, worded the way a person would say it.
   *
   * A day close to today gets its name; anything further away gets its date, because "in nine
   * days" is a worse answer than "August 4".
   */
  protected readonly title = computed(() => {
    const relative = relativeDayName(this.date(), this.todayDate());

    return relative === null
      ? `Agenda for ${this.shortLabel()}`
      : `${relative}'s agenda`;
  });

  /** The date spelled out, shown under the title when the title does not already contain it. */
  protected readonly showDateSubtitle = computed(
    () => relativeDayName(this.date(), this.todayDate()) !== null,
  );

  protected readonly isEmpty = computed(
    () =>
      !this.isLoading()
      && this.error() === null
      && this.anytime().length === 0
      && this.scheduled().length === 0,
  );

  /** Whether the day holds anything at all, as opposed to nothing matching the filter. */
  protected readonly isFilteredToNothing = computed(
    () => this.isEmpty() && this.hiddenCount() > 0,
  );

  private readonly shortLabel = computed(() => {
    const label = this.dateLabel();
    const comma = label.indexOf(', ');

    // "Tuesday, August 4" reads better as "August 4" once the sentence already has a subject.
    return comma === -1 ? label : label.slice(comma + 2);
  });
}
