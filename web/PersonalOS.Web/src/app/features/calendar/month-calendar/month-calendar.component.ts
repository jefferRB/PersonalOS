import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';

import { kindPresentation } from '../../../core/calendar/activity-visuals';
import { CalendarDaySummary, DayKindCount } from '../../../core/calendar/calendar.models';
import {
  CalendarDisplay,
  SLOT_MINUTE_OPTIONS,
  UpdateCalendarDisplayRequest,
} from '../../../core/profile/profile.models';
import {
  IsoLocalDate,
  MonthGridCell,
  WEEKDAY_HEADERS,
  addDays,
  buildMonthCells,
  clampToMonth,
  endOfMonth,
  formatDayLabel,
  formatMonthTitle,
  startOfMonth,
  toInputTime,
  toMinutesOfDay,
} from '../../../core/time/local-date';
import { MonthKey } from '../calendar.store';

/** Month names in the explicit English locale the application renders dates with. */
const MONTH_NAMES: readonly string[] = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
];

/** How far either side of the shown year the year picker offers. */
const YEAR_RADIUS = 5;

/**
 * The month card: its own controls, then the grid.
 *
 * The controls live inside the card rather than floating above the page, because they only ever act
 * on this grid and the planner it opens. Two compact rows keep them out of the way: what to look at
 * on top, which month underneath.
 *
 * The grid holds only the days of the visible month. Leading and trailing positions are blanks, not
 * the neighbouring months' dates, so a month never looks like it starts on the 29th and a click can
 * never jump somewhere the user did not ask to go. Blanks are inert: no focus, no name, nothing
 * announced.
 *
 * Keyboard navigation uses a roving tab stop and steps over the blanks, stopping at the edges of
 * the month rather than dragging the grid to another one.
 */
@Component({
  selector: 'app-month-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './month-calendar.component.html',
  styleUrl: './month-calendar.component.scss',
})
export class MonthCalendarComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);

  /** First day of the month being shown, as `yyyy-MM-dd`. */
  readonly monthAnchor = input.required<IsoLocalDate>();

  /** Which month the grid is showing, as the API addresses it. */
  readonly anchor = input.required<MonthKey>();

  /** The day the agenda is pointing at. */
  readonly selectedDate = input.required<IsoLocalDate>();

  /** The account's current local day, as decided by the server, or `null` until it is known. */
  readonly todayDate = input<IsoLocalDate | null>(null);

  /** Day summaries keyed by date. */
  readonly summaries = input.required<ReadonlyMap<IsoLocalDate, CalendarDaySummary>>();

  /** How the planner's timeline is currently drawn. */
  readonly display = input.required<CalendarDisplay>();

  readonly isSavingDisplay = input(false);

  /** Why the server refused the last display change, if it did. */
  readonly displayServerError = input<string | null>(null);

  readonly isLoading = input(false);

  readonly error = input<string | null>(null);

  /** The user picked a day. */
  readonly daySelected = output<IsoLocalDate>();

  /** The user moved the grid by whole months. */
  readonly monthOffset = output<number>();

  /** The user picked a month or a year directly. */
  readonly monthSelected = output<MonthKey>();

  /** The user asked to open the planner on the selected day. */
  readonly planDay = output<void>();

  /** The user applied a new timeline configuration. */
  readonly displayApplied = output<UpdateCalendarDisplayRequest>();

  /** The user asked to retry a failed load. */
  readonly retry = output<void>();

  protected readonly weekdayHeaders = WEEKDAY_HEADERS;
  protected readonly months = MONTH_NAMES.map((label, index) => ({ value: index + 1, label }));
  protected readonly slotOptions = SLOT_MINUTE_OPTIONS;

  /** A local message when the values cannot make a timeline, shown before anything is sent. */
  protected readonly localDisplayError = signal<string | null>(null);

  protected readonly displayForm = this.formBuilder.group({
    startTime: this.formBuilder.control(''),
    endTime: this.formBuilder.control(''),
    slotMinutes: this.formBuilder.control(15),
  });

  protected readonly monthTitle = computed(() => formatMonthTitle(this.monthAnchor()));

  protected readonly cells = computed(() => buildMonthCells(this.monthAnchor()));

  protected readonly years = computed(() => {
    const current = this.anchor().year;

    return Array.from({ length: YEAR_RADIUS * 2 + 1 }, (_, index) => current - YEAR_RADIUS + index);
  });

  protected readonly displayError = computed(
    () => this.localDisplayError() ?? this.displayServerError(),
  );

  /**
   * Which day carries the grid's single tab stop.
   *
   * It is the selected day when that day is in view, and the first of the month otherwise, so
   * paging to another month always leaves exactly one reachable cell.
   */
  protected readonly focusDate = computed(() =>
    clampToMonth(this.selectedDate(), this.monthAnchor()),
  );

  constructor() {
    // The form mirrors what is saved. Resetting from the input rather than once at construction
    // keeps it right after a save, after a reload, and after a failed attempt.
    effect(() => {
      const display = this.display();

      this.displayForm.reset({
        startTime: toInputTime(display.dayStartTime),
        endTime: toInputTime(display.dayEndTime),
        slotMinutes: display.slotMinutes,
      });
      this.localDisplayError.set(null);
    });
  }

  protected summaryFor(date: IsoLocalDate): CalendarDaySummary | undefined {
    return this.summaries().get(date);
  }

  protected isSelected(date: IsoLocalDate): boolean {
    return date === this.selectedDate();
  }

  protected isToday(date: IsoLocalDate): boolean {
    return date === this.todayDate();
  }

  protected isFocusable(date: IsoLocalDate): boolean {
    return date === this.focusDate();
  }

  protected iconPathFor(kind: DayKindCount): string {
    return kindPresentation(kind.kind).iconPath;
  }

  protected kindTitle(kind: DayKindCount): string {
    const presentation = kindPresentation(kind.kind);

    return kind.count === 1
      ? `1 ${presentation.label.toLowerCase()}`
      : `${kind.count} ${presentation.label.toLowerCase()}s`;
  }

  /**
   * The name a screen reader announces for one day.
   *
   * It carries the date, the totals, the kinds, the outcomes, and whether anything is important,
   * because the badges and glyphs convey all of that visually and none of it otherwise.
   */
  protected dayLabel(date: IsoLocalDate): string {
    const summary = this.summaryFor(date);
    const dayName = formatDayLabel(date);

    if (summary === undefined || summary.totalCount === 0) {
      return `${dayName}, nothing scheduled`;
    }

    const parts = [summary.totalCount === 1 ? '1 activity' : `${summary.totalCount} activities`];

    for (const kind of summary.kinds) {
      parts.push(this.kindTitle(kind));
    }

    if (summary.completedCount > 0) {
      parts.push(`${summary.completedCount} completed`);
    }

    if (summary.failedCount > 0) {
      parts.push(`${summary.failedCount} failed`);
    }

    if (summary.cancelledCount > 0) {
      parts.push(`${summary.cancelledCount} cancelled`);
    }

    if (summary.hasHighPriority) {
      parts.push('includes something important');
    }

    return `${dayName}, ${parts.join(', ')}`;
  }

  protected onMonthChange(value: string): void {
    this.monthSelected.emit({ year: this.anchor().year, month: Number(value) });
  }

  protected onYearChange(value: string): void {
    this.monthSelected.emit({ year: Number(value), month: this.anchor().month });
  }

  /**
   * Applies the timeline configuration.
   *
   * An invalid range is reported rather than corrected. Silently swapping the two values, or
   * snapping them to something legal, leaves the user looking at a timeline they did not ask for
   * with no explanation of why.
   */
  protected onApplyDisplay(): void {
    if (this.isSavingDisplay()) {
      return;
    }

    const value = this.displayForm.getRawValue();
    const start = toMinutesOfDay(value.startTime);
    const end = toMinutesOfDay(value.endTime);

    if (start === null || end === null) {
      this.localDisplayError.set('Enter a start time and an end time.');

      return;
    }

    if (start >= end) {
      this.localDisplayError.set('The start time must be earlier than the end time.');

      return;
    }

    this.localDisplayError.set(null);
    this.displayApplied.emit({
      dayStartTime: value.startTime,
      dayEndTime: value.endTime,
      slotMinutes: Number(value.slotMinutes),
    });
  }

  protected onDayKeydown(event: KeyboardEvent): void {
    const moves: Record<string, number> = {
      ArrowRight: 1,
      ArrowLeft: -1,
      ArrowDown: 7,
      ArrowUp: -7,
    };

    const anchor = this.monthAnchor();
    const offset = moves[event.key];

    if (offset !== undefined) {
      event.preventDefault();
      // Clamping keeps the move inside the visible month, so a blank is never landed on and the
      // grid never pages out from under the key that is being held down.
      this.daySelected.emit(clampToMonth(addDays(this.focusDate(), offset), anchor));

      return;
    }

    if (event.key === 'Home') {
      event.preventDefault();
      this.daySelected.emit(startOfMonth(anchor));
    }

    if (event.key === 'End') {
      event.preventDefault();
      this.daySelected.emit(endOfMonth(anchor));
    }
  }

  /** Narrows a grid cell for the template, which cannot do it on its own. */
  protected asDay(cell: MonthGridCell): { date: IsoLocalDate; dayOfMonth: number } | null {
    return cell.kind === 'day' ? cell : null;
  }

  protected cellKey(cell: MonthGridCell): string {
    return cell.kind === 'day' ? cell.date : cell.key;
  }
}
