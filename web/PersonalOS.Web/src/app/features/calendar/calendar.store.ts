import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, of, switchMap, tap } from 'rxjs';

import {
  CalendarDay,
  CalendarMonth,
  OccurrenceStatus,
  PlanningItem,
  SavePlanningItemRequest,
  UpcomingWeek,
} from '../../core/calendar/calendar.models';
import { CalendarService } from '../../core/calendar/calendar.service';
import {
  DEFAULT_DAY_FILTER,
  DEFAULT_UPCOMING_FILTER,
  OccurrenceFilter,
  filterOccurrences,
  isDefaultFilter,
  splitByTime,
} from '../../core/calendar/occurrence-filters';
import { TimelineWindow } from '../../core/calendar/timeline-layout';
import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import {
  CalendarDisplay,
  DEFAULT_CALENDAR_DISPLAY,
  UpdateCalendarDisplayRequest,
} from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import {
  IsoLocalDate,
  addDays,
  addMonths,
  startOfMonth,
  toIsoLocalDate,
  toMinutesOfDay,
} from '../../core/time/local-date';

/** One month, addressed the way the API addresses it. */
export interface MonthKey {
  readonly year: number;
  readonly month: number;
}

/** What a screen needs to know about a request that may still be running. */
export interface AsyncState<TValue> {
  readonly value: TValue | null;
  readonly isLoading: boolean;
  readonly error: string | null;
}

/** What the activity editor is currently doing. */
export type EditorMode = 'closed' | 'create' | 'edit';

/** The editor's state, held here so the planner can close it without closing itself. */
export interface EditorState {
  readonly mode: EditorMode;
  /** The item being edited, once it has loaded. */
  readonly item: PlanningItem | null;
  /** Time a newly created activity should start at, from the slot that was clicked. */
  readonly defaultTime: string | null;
  readonly isLoading: boolean;
  readonly error: string | null;
  readonly fieldErrors: Record<string, string[]>;
}

const CLOSED_EDITOR: EditorState = {
  mode: 'closed',
  item: null,
  defaultTime: null,
  isLoading: false,
  error: null,
  fieldErrors: {},
};

function idle<TValue>(): AsyncState<TValue> {
  return { value: null, isLoading: false, error: null };
}

/**
 * The calendar page's own state.
 *
 * The store is provided by the page rather than at the root, so its lifetime is the screen's
 * lifetime and nothing survives navigation. Calendar data is never written to browser storage:
 * where somebody will be and when is exactly the sort of thing that must not outlive the tab.
 *
 * The month, the day, and the seven-day window load independently. Each keeps its own loading and
 * error state, because a failed month must not blank out an agenda that arrived perfectly well.
 *
 * Every load goes through a subject and `switchMap`, which is what makes navigation safe: holding
 * a month arrow down fires a request per month, and `switchMap` unsubscribes from all but the last,
 * so a slow early response can never overwrite the month the user is actually looking at.
 *
 * Filtering happens in computed signals over the loaded data. A day and a week are bounded, so
 * changing a filter is arithmetic rather than a round trip, and the stored lists are never edited.
 */
@Injectable()
export class CalendarStore {
  private readonly service = inject(CalendarService);
  private readonly profile = inject(ProfileService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly monthRequests = new Subject<MonthKey>();
  private readonly dayRequests = new Subject<IsoLocalDate | undefined>();
  private readonly upcomingRequests = new Subject<IsoLocalDate | undefined>();

  private readonly monthState = signal<AsyncState<CalendarMonth>>(idle());
  private readonly dayState = signal<AsyncState<CalendarDay>>(idle());
  private readonly upcomingState = signal<AsyncState<UpcomingWeek>>(idle());

  private readonly anchorSignal = signal<MonthKey>(currentMonthKey());
  private readonly selectedDateSignal = signal<IsoLocalDate>(toIsoLocalDate(new Date()));
  private readonly plannerDateSignal = signal<IsoLocalDate | null>(null);
  private readonly dayFilterSignal = signal<OccurrenceFilter>(DEFAULT_DAY_FILTER);
  private readonly upcomingFilterSignal = signal<OccurrenceFilter>(DEFAULT_UPCOMING_FILTER);
  private readonly displaySignal = signal<CalendarDisplay>(DEFAULT_CALENDAR_DISPLAY);
  private readonly displayErrorSignal = signal<string | null>(null);
  private readonly isSavingDisplaySignal = signal(false);
  private readonly editorSignal = signal<EditorState>(CLOSED_EDITOR);
  private readonly savingSignal = signal(false);
  private readonly deletingSignal = signal(false);
  private readonly busyItemSignal = signal<string | null>(null);
  private readonly announcementSignal = signal('');
  private readonly actionErrorSignal = signal<string | null>(null);

  /** The month grid, with its own loading and error state. */
  readonly month = this.monthState.asReadonly();

  /** The selected day's occurrences, with their own loading and error state. */
  readonly day = this.dayState.asReadonly();

  /** The next seven days, with their own loading and error state. */
  readonly upcoming = this.upcomingState.asReadonly();

  /** Which month the grid is showing. */
  readonly anchor = this.anchorSignal.asReadonly();

  /** Which day the agenda is showing. */
  readonly selectedDate = this.selectedDateSignal.asReadonly();

  /** Which day the planner is open on, or `null` when it is closed. */
  readonly plannerDate = this.plannerDateSignal.asReadonly();

  /** What the daily agenda is filtered to. */
  readonly dayFilter = this.dayFilterSignal.asReadonly();

  /** What the seven-day section is filtered to. */
  readonly upcomingFilter = this.upcomingFilterSignal.asReadonly();

  /** How the planner's timeline is shown. */
  readonly display = this.displaySignal.asReadonly();

  /** Why the last display change was refused, if it was. */
  readonly displayError = this.displayErrorSignal.asReadonly();

  readonly isSavingDisplay = this.isSavingDisplaySignal.asReadonly();

  /** What the activity editor is doing. */
  readonly editor = this.editorSignal.asReadonly();

  readonly isSaving = this.savingSignal.asReadonly();

  readonly isDeleting = this.deletingSignal.asReadonly();

  /** Which item has a status change in flight, so its controls can be disabled. */
  readonly busyItemId = this.busyItemSignal.asReadonly();

  /** The most recent success message, announced through the page's live region. */
  readonly announcement = this.announcementSignal.asReadonly();

  /** A failure that belongs to the page rather than to one section. */
  readonly actionError = this.actionErrorSignal.asReadonly();

  /** Whether the planner is open. */
  readonly isPlannerOpen = computed(() => this.plannerDateSignal() !== null);

  /**
   * The account's current local day, as decided by the server.
   *
   * It comes from whichever response arrived, never from the browser clock, so "Today" stays
   * correct for a user whose device is in another time zone. Until the first response lands there
   * is no answer, and the screen shows none rather than guessing.
   */
  readonly todayLocalDate = computed<IsoLocalDate | null>(
    () =>
      this.dayState().value?.todayLocalDate
      ?? this.monthState().value?.todayLocalDate
      ?? this.upcomingState().value?.todayLocalDate
      ?? null,
  );

  /** Whether the agenda is already showing the account's current day. */
  readonly isViewingToday = computed(
    () => this.todayLocalDate() !== null && this.selectedDateSignal() === this.todayLocalDate(),
  );

  /** The first day of the month the grid is showing, as `yyyy-MM-dd`. */
  readonly monthAnchorDate = computed<IsoLocalDate>(() => {
    const { year, month } = this.anchorSignal();

    return `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}-01`;
  });

  /** Day summaries keyed by date, so a grid cell is a lookup rather than a scan. */
  readonly summariesByDate = computed(
    () => new Map((this.monthState().value?.days ?? []).map((day) => [day.date, day])),
  );

  /** The visible window and resolution the timeline is drawn at. */
  readonly timelineWindow = computed<TimelineWindow>(() => {
    const display = this.displaySignal();

    return {
      startMinutes: toMinutesOfDay(display.dayStartTime) ?? 6 * 60,
      endMinutes: toMinutesOfDay(display.dayEndTime) ?? 22 * 60,
      intervalMinutes: display.slotMinutes,
    };
  });

  /** Everything on the selected day, before the agenda's filters. */
  readonly dayOccurrences = computed(() => this.dayState().value?.occurrences ?? []);

  /** The selected day after the agenda's filters, split into untimed and timed. */
  readonly filteredDay = computed(() =>
    splitByTime(filterOccurrences(this.dayOccurrences(), this.dayFilterSignal())),
  );

  /** How many of the day's occurrences the current filter is hiding. */
  readonly hiddenDayCount = computed(
    () =>
      this.dayOccurrences().length
      - this.filteredDay().anytime.length
      - this.filteredDay().scheduled.length,
  );

  /** The seven-day window after its filters, grouped by day with empty days dropped. */
  readonly filteredUpcoming = computed(() => {
    const filter = this.upcomingFilterSignal();

    return (this.upcomingState().value?.days ?? [])
      .map((day) => ({
        date: day.date,
        ...splitByTime(filterOccurrences(day.occurrences, filter)),
      }))
      .filter((day) => day.anytime.length > 0 || day.scheduled.length > 0);
  });

  /** Whether the agenda's filters still match their defaults. */
  readonly isDayFilterDefault = computed(() =>
    isDefaultFilter(this.dayFilterSignal(), DEFAULT_DAY_FILTER),
  );

  /** Whether the seven-day filters still match their defaults. */
  readonly isUpcomingFilterDefault = computed(() =>
    isDefaultFilter(this.upcomingFilterSignal(), DEFAULT_UPCOMING_FILTER),
  );

  constructor() {
    this.monthRequests
      .pipe(
        tap(() => this.monthState.update((state) => ({ ...state, isLoading: true, error: null }))),
        switchMap(({ year, month }) =>
          this.service.getMonth(year, month).pipe(
            catchError((error: unknown) => {
              this.monthState.set({
                value: null,
                isLoading: false,
                error: formLevelMessage(toApiError(error)),
              });

              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => {
        if (value !== null) {
          this.monthState.set({ value, isLoading: false, error: null });
        }
      });

    this.dayRequests
      .pipe(
        tap(() => this.dayState.update((state) => ({ ...state, isLoading: true, error: null }))),
        switchMap((date) =>
          this.service.getDay(date).pipe(
            catchError((error: unknown) => {
              this.dayState.set({
                value: null,
                isLoading: false,
                error: formLevelMessage(toApiError(error)),
              });

              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => {
        if (value === null) {
          return;
        }

        this.dayState.set({ value, isLoading: false, error: null });

        // The response is authoritative about which day it describes. On the first load the request
        // carries no date at all, so this is where the agenda learns the account's real current day
        // and the grid follows it to the right month.
        this.selectedDateSignal.set(value.date);
        this.syncAnchorTo(value.date);
      });

    this.upcomingRequests
      .pipe(
        tap(() =>
          this.upcomingState.update((state) => ({ ...state, isLoading: true, error: null })),
        ),
        switchMap((from) =>
          this.service.getUpcoming(from).pipe(
            catchError((error: unknown) => {
              this.upcomingState.set({
                value: null,
                isLoading: false,
                error: formLevelMessage(toApiError(error)),
              });

              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => {
        if (value !== null) {
          this.upcomingState.set({ value, isLoading: false, error: null });
        }
      });
  }

  /**
   * Loads the screen for the first time.
   *
   * The day is requested without a date so the server decides which day is current, and the answer
   * then anchors the agenda and the grid. That is why the calendar opens on the right day for a
   * user whose laptop is still set to the time zone they flew in from.
   */
  initialize(): void {
    this.dayRequests.next(undefined);
    this.upcomingRequests.next(undefined);
    this.reloadMonth();
    this.loadDisplayPreferences();
  }

  /** Points the agenda at a day, reloading what depends on it. */
  selectDate(date: IsoLocalDate): void {
    if (this.selectedDateSignal() === date) {
      return;
    }

    this.selectedDateSignal.set(date);
    this.dayRequests.next(date);
    this.syncAnchorTo(date);

    // The planner shows whatever the agenda shows while it is open.
    if (this.plannerDateSignal() !== null) {
      this.plannerDateSignal.set(date);
    }
  }

  /** Moves the grid a whole month at a time without moving the agenda. */
  goToMonth(offset: number): void {
    this.setAnchor(monthKeyOf(addMonths(this.monthAnchorDate(), offset)));
  }

  /**
   * Points the grid straight at a month, which is what the month and year pickers do.
   *
   * It goes through the same path as the arrows, so a fast sequence of picks is protected from
   * stale responses in exactly the same way.
   */
  setMonth(year: number, month: number): void {
    this.setAnchor({ year, month });
  }

  /**
   * Returns to the account's current local day.
   *
   * Does nothing until the server has said which day that is, because guessing from the browser
   * clock is exactly the mistake this screen avoids everywhere else.
   */
  goToToday(): void {
    const today = this.todayLocalDate();

    if (today !== null) {
      this.selectDate(today);
    }
  }

  /** Moves the agenda one day at a time. */
  shiftSelectedDate(offsetDays: number): void {
    this.selectDate(addDays(this.selectedDateSignal(), offsetDays));
  }

  /** Opens the planner on the day the agenda is showing. */
  openPlanner(date?: IsoLocalDate): void {
    if (date !== undefined) {
      this.selectDate(date);
    }

    this.plannerDateSignal.set(this.selectedDateSignal());
  }

  /** Closes the planner. The selected day and the editor are both reset deliberately. */
  closePlanner(): void {
    this.plannerDateSignal.set(null);
    this.closeEditor();
  }

  setDayFilter(filter: OccurrenceFilter): void {
    this.dayFilterSignal.set(filter);
  }

  resetDayFilter(): void {
    this.dayFilterSignal.set(DEFAULT_DAY_FILTER);
  }

  setUpcomingFilter(filter: OccurrenceFilter): void {
    this.upcomingFilterSignal.set(filter);
  }

  resetUpcomingFilter(): void {
    this.upcomingFilterSignal.set(DEFAULT_UPCOMING_FILTER);
  }

  /** Opens the editor for a new activity, optionally at the time of a clicked slot. */
  openCreateEditor(defaultTime: string | null = null): void {
    this.editorSignal.set({ ...CLOSED_EDITOR, mode: 'create', defaultTime });
  }

  /**
   * Opens the editor for an existing activity.
   *
   * The occurrence on screen carries no recurrence rule, so the full item is fetched first. Editing
   * a repeating activity without knowing its rule would let a save silently rewrite it.
   */
  openEditEditor(planningItemId: string): void {
    this.editorSignal.set({ ...CLOSED_EDITOR, mode: 'edit', isLoading: true });

    this.service
      .getItem(planningItemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (item) =>
          this.editorSignal.update((state) => ({ ...state, item, isLoading: false })),
        error: (error: unknown) =>
          this.editorSignal.update((state) => ({
            ...state,
            isLoading: false,
            error: formLevelMessage(toApiError(error)),
          })),
      });
  }

  /** Closes the editor without touching the planner. */
  closeEditor(): void {
    this.editorSignal.set(CLOSED_EDITOR);
  }

  reloadMonth(): void {
    this.monthRequests.next(this.anchorSignal());
  }

  reloadDay(): void {
    this.dayRequests.next(this.selectedDateSignal());
  }

  reloadUpcoming(): void {
    this.upcomingRequests.next(undefined);
  }

  clearAnnouncement(): void {
    this.announcementSignal.set('');
  }

  clearActionError(): void {
    this.actionErrorSignal.set(null);
  }

  /** Loads how this account wants the planner's timeline shown. */
  loadDisplayPreferences(): void {
    this.profile
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => this.displaySignal.set(profile.calendarDisplay),
        // A profile that will not load leaves the defaults in place. The calendar is still usable
        // with them, so a failure here must not take the page down with it.
        error: () => this.displaySignal.set(DEFAULT_CALENDAR_DISPLAY),
      });
  }

  /**
   * Saves how the planner's timeline is shown.
   *
   * An invalid range is refused with a message rather than corrected, so the user finds out that
   * their choice was rejected instead of quietly getting a different one.
   */
  saveDisplayPreferences(request: UpdateCalendarDisplayRequest): void {
    if (this.isSavingDisplaySignal()) {
      return;
    }

    this.displayErrorSignal.set(null);
    this.isSavingDisplaySignal.set(true);

    this.profile
      .updateCalendarDisplay(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.isSavingDisplaySignal.set(false);
          this.displaySignal.set(profile.calendarDisplay);
          this.announce('Timeline settings saved.');
        },
        error: (error: unknown) => {
          this.isSavingDisplaySignal.set(false);

          const apiError = toApiError(error);
          const firstField = Object.values(apiError.validationErrors)[0]?.[0];

          this.displayErrorSignal.set(firstField ?? formLevelMessage(apiError));
        },
      });
  }

  /** Creates or edits an item, then refreshes only what the change could have touched. */
  save(request: SavePlanningItemRequest, onSuccess: () => void): void {
    if (this.savingSignal()) {
      return;
    }

    const editing = this.editorSignal().item;

    this.savingSignal.set(true);
    this.editorSignal.update((state) => ({ ...state, error: null, fieldErrors: {} }));

    const call =
      editing === null
        ? this.service.create(request)
        : this.service.update(editing.id, request);

    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (item) => {
        this.savingSignal.set(false);
        this.announce(editing === null ? 'Activity created.' : 'Activity updated.');
        // Both the day it moved from and the day it moved to may need refreshing.
        this.refreshFor(item.startDate);
        this.refreshFor(editing?.startDate ?? item.startDate);
        onSuccess();
      },
      error: (error: unknown) => {
        this.savingSignal.set(false);
        this.applyEditorError(error);
      },
    });
  }

  /** Deletes an item and the whole series it stands for. */
  delete(itemId: string, startDate: IsoLocalDate, onSuccess: () => void): void {
    if (this.deletingSignal()) {
      return;
    }

    this.deletingSignal.set(true);

    this.service
      .delete(itemId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deletingSignal.set(false);
          this.announce('Activity deleted.');
          // A series can span the whole grid, so every section is refreshed after a delete.
          this.refreshAll();
          onSuccess();
        },
        error: (error: unknown) => {
          this.deletingSignal.set(false);
          this.applyEditorError(error);
        },
      });
  }

  /** Records what the user decided about one occurrence. */
  setOccurrenceStatus(
    itemId: string,
    occurrenceDate: IsoLocalDate,
    status: OccurrenceStatus,
  ): void {
    if (this.busyItemSignal() !== null) {
      return;
    }

    this.busyItemSignal.set(itemId);
    this.actionErrorSignal.set(null);

    this.service
      .setOccurrenceStatus(itemId, occurrenceDate, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (occurrence) => {
          this.busyItemSignal.set(null);
          this.announce(`${occurrence.title} marked ${status}.`);
          this.refreshFor(occurrenceDate);
        },
        error: (error: unknown) => {
          this.busyItemSignal.set(null);
          this.actionErrorSignal.set(formLevelMessage(toApiError(error)));
        },
      });
  }

  /**
   * Refreshes only the sections one date can appear in.
   *
   * A completion on a day nobody is looking at should not cost three requests, and reloading the
   * whole screen after every checkbox would make the page flicker for no reason.
   */
  private refreshFor(date: IsoLocalDate): void {
    if (date === this.selectedDateSignal()) {
      this.reloadDay();
    }

    const month = this.monthState().value;

    if (month === null || (date >= month.fromDate && date <= month.toDate)) {
      this.reloadMonth();
    }

    const upcoming = this.upcomingState().value;

    if (upcoming === null || (date >= upcoming.fromDate && date <= upcoming.toDate)) {
      this.reloadUpcoming();
    }
  }

  private refreshAll(): void {
    this.reloadDay();
    this.reloadMonth();
    this.reloadUpcoming();
  }

  private setAnchor(anchor: MonthKey): void {
    this.anchorSignal.set(anchor);
    this.reloadMonth();
  }

  private syncAnchorTo(date: IsoLocalDate): void {
    const anchor = monthKeyOf(date);
    const current = this.anchorSignal();

    if (anchor.year !== current.year || anchor.month !== current.month) {
      this.setAnchor(anchor);
    }
  }

  private applyEditorError(error: unknown): void {
    const apiError = toApiError(error);
    const hasFieldErrors = Object.keys(apiError.validationErrors).length > 0;

    this.editorSignal.update((state) => ({
      ...state,
      fieldErrors: apiError.validationErrors,
      error: hasFieldErrors ? null : formLevelMessage(apiError),
    }));
  }

  private announce(message: string): void {
    this.announcementSignal.set(message);
  }
}

function currentMonthKey(): MonthKey {
  return monthKeyOf(toIsoLocalDate(new Date()));
}

function monthKeyOf(date: IsoLocalDate): MonthKey {
  const first = startOfMonth(date);

  return { year: Number(first.slice(0, 4)), month: Number(first.slice(5, 7)) };
}
