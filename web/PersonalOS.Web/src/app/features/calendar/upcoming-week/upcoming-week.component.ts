import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { CalendarOccurrence } from '../../../core/calendar/calendar.models';
import {
  OccurrenceFilter,
  UPCOMING_VIEW_OPTIONS,
} from '../../../core/calendar/occurrence-filters';
import { IsoLocalDate, formatDayLabel, relativeDayName } from '../../../core/time/local-date';
import { ActivityCardComponent } from '../activity-card/activity-card.component';
import { OccurrenceFiltersComponent } from '../occurrence-filters/occurrence-filters.component';

/** One day of the section, already filtered and split. */
export interface UpcomingGroup {
  readonly date: IsoLocalDate;
  readonly anytime: readonly CalendarOccurrence[];
  readonly scheduled: readonly CalendarOccurrence[];
}

/**
 * The next seven days.
 *
 * Important-only is on by default, which reproduces exactly what this section always showed:
 * events and appointments, plus the tasks and routines the user marked important. Turning it off
 * reveals the rest of the week without another request, because the whole window is already loaded.
 */
@Component({
  selector: 'app-upcoming-week',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ActivityCardComponent, OccurrenceFiltersComponent],
  templateUrl: './upcoming-week.component.html',
  styleUrl: './upcoming-week.component.scss',
})
export class UpcomingWeekComponent {
  readonly groups = input.required<readonly UpcomingGroup[]>();

  /** The account's current local day, as decided by the server. */
  readonly todayDate = input<IsoLocalDate | null>(null);

  readonly filter = input.required<OccurrenceFilter>();

  readonly isFilterDefault = input(true);

  readonly isLoading = input(false);

  readonly error = input<string | null>(null);

  readonly open = output<CalendarOccurrence>();

  /** The user asked to jump the calendar to one of these days. */
  readonly daySelected = output<IsoLocalDate>();

  readonly filterChange = output<OccurrenceFilter>();

  readonly filterCleared = output<void>();

  readonly retry = output<void>();

  protected readonly viewOptions = UPCOMING_VIEW_OPTIONS;

  protected readonly isEmpty = computed(
    () => !this.isLoading() && this.error() === null && this.groups().length === 0,
  );

  /** Names a day the way a person would, falling back to its date. */
  protected label(date: IsoLocalDate): string {
    const relative = relativeDayName(date, this.todayDate());
    const formatted = formatDayLabel(date);

    return relative === null ? formatted : `${relative} - ${formatted}`;
  }
}
