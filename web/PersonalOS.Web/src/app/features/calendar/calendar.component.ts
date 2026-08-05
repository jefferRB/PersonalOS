import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { KIND_PRESENTATIONS_IN_ORDER } from '../../core/calendar/activity-visuals';
import { CalendarOccurrence } from '../../core/calendar/calendar.models';
import { OccurrenceFilter } from '../../core/calendar/occurrence-filters';
import { UpdateCalendarDisplayRequest } from '../../core/profile/profile.models';
import { IsoLocalDate } from '../../core/time/local-date';
import { OccurrenceStatusChange } from './activity-card/activity-card.component';
import { CalendarStore, MonthKey } from './calendar.store';
import { DailyAgendaComponent } from './daily-agenda/daily-agenda.component';
import { DayPlannerComponent } from './day-planner/day-planner.component';
import { UpcomingWeekComponent } from './upcoming-week/upcoming-week.component';
import { MonthCalendarComponent } from './month-calendar/month-calendar.component';

/**
 * The calendar: a month, the day beside it, and what is coming next.
 *
 * The calendar is for planning; Today is for executing. Both read the same occurrence projection
 * from the server, so an activity created here is the same row Today completes, with no second task
 * model to keep in step.
 *
 * The three sections load independently and each keeps its own loading and error state, so a failed
 * month never blanks out an agenda that arrived perfectly well. Filtering runs over what is already
 * loaded, because a day and a week are bounded and a filter should not cost a round trip.
 */
@Component({
  selector: 'app-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CalendarStore],
  imports: [
    DailyAgendaComponent,
    DayPlannerComponent,
    MonthCalendarComponent,
    UpcomingWeekComponent,
  ],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.scss',
})
export class CalendarComponent {
  protected readonly store = inject(CalendarStore);

  protected readonly kindLegend = KIND_PRESENTATIONS_IN_ORDER;

  protected readonly canGoToToday = computed(() => this.store.todayLocalDate() !== null);

  constructor() {
    this.store.initialize();
  }

  /** Picking a day moves the agenda and opens the planner on it. */
  protected onDaySelected(date: IsoLocalDate): void {
    this.store.openPlanner(date);
  }

  /** Jumping from the seven-day list moves the calendar without opening the planner. */
  protected onJumpToDate(date: IsoLocalDate): void {
    this.store.selectDate(date);
  }

  protected onMonthOffset(offset: number): void {
    this.store.goToMonth(offset);
  }

  protected onMonthSelected(anchor: MonthKey): void {
    this.store.setMonth(anchor.year, anchor.month);
  }

  protected onDayOffset(offset: number): void {
    this.store.shiftSelectedDate(offset);
  }

  protected onGoToToday(): void {
    this.store.goToToday();
  }

  protected onDisplayApplied(request: UpdateCalendarDisplayRequest): void {
    this.store.saveDisplayPreferences(request);
  }

  protected onStatusChange(change: OccurrenceStatusChange): void {
    this.store.setOccurrenceStatus(
      change.occurrence.planningItemId,
      change.occurrence.occurrenceDate,
      change.status,
    );
  }

  /** Opening an activity goes through the planner, which owns the editor. */
  protected onOpenOccurrence(occurrence: CalendarOccurrence): void {
    this.store.openPlanner(occurrence.occurrenceDate);
    this.store.openEditEditor(occurrence.planningItemId);
  }

  protected onDayFilterChange(filter: OccurrenceFilter): void {
    this.store.setDayFilter(filter);
  }

  protected onUpcomingFilterChange(filter: OccurrenceFilter): void {
    this.store.setUpcomingFilter(filter);
  }
}
