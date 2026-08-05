import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable, finalize, take } from 'rxjs';

import { AuthStore } from '../../core/auth/auth.store';
import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import {
  parseInteger,
  requiredInteger,
  trimToNull,
  trimValue,
  trimmedLength,
} from '../../core/forms/validators';
import { statusLabel } from '../../core/calendar/activity-visuals';
import {
  CalendarOccurrence,
  OccurrenceStatus,
  PLANNING_CATEGORIES,
  PlanningCategory,
} from '../../core/calendar/calendar.models';
import { CalendarService } from '../../core/calendar/calendar.service';
import { MEAL_TYPES, MealType } from '../../core/nutrition/nutrition.models';
import { NutritionService } from '../../core/nutrition/nutrition.service';
import { StudyProject } from '../../core/study/study.models';
import { StudyService } from '../../core/study/study.service';
import { formatEnglishLocalDate } from '../../core/time/english-date';
import { formatMinutes, formatTimeLabel } from '../../core/time/local-date';
import { TodaySummary } from '../../core/today/today.models';
import { TodayService } from '../../core/today/today.service';

/** Which quick-add form is open, if any. */
type QuickAddMode = 'none' | 'task' | 'meal' | 'study';

/**
 * The main operating screen: one local day, seen from every module at once.
 *
 * Today reads a single aggregate endpoint rather than calling five services, so the page never
 * renders a half-built day. After any change it reloads that one endpoint, which is what keeps
 * the summary numbers from disagreeing with the lists beneath them.
 *
 * Every number shown here is counted from data the user entered. Nothing is estimated, and no
 * streak or score is invented to fill space.
 */
@Component({
  selector: 'app-today',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './today.component.html',
  styleUrl: './today.component.scss',
})
export class TodayComponent {
  private readonly authStore = inject(AuthStore);
  private readonly todayService = inject(TodayService);
  private readonly calendarService = inject(CalendarService);
  private readonly nutritionService = inject(NutritionService);
  private readonly studyService = inject(StudyService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly categories = PLANNING_CATEGORIES;
  protected readonly mealTypes = MEAL_TYPES;

  protected readonly currentUser = this.authStore.currentUser;
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly summary = signal<TodaySummary | null>(null);

  protected readonly quickAddMode = signal<QuickAddMode>('none');
  protected readonly isSaving = signal(false);
  protected readonly quickAddError = signal<string | null>(null);
  protected readonly quickAddSuccess = signal<string | null>(null);
  protected readonly studyProjects = signal<readonly StudyProject[]>([]);

  /** Identifier of the item whose status change is in flight, so its button can be disabled. */
  protected readonly pendingItemId = signal<string | null>(null);

  protected readonly taskForm = this.formBuilder.group({
    title: this.formBuilder.control('', [trimmedLength(1, 200)]),
    startTime: this.formBuilder.control(''),
    category: this.formBuilder.control<PlanningCategory>('general'),
  });

  protected readonly mealForm = this.formBuilder.group({
    name: this.formBuilder.control('', [trimmedLength(1, 200)]),
    calories: this.formBuilder.control('', [requiredInteger(0, 20000)]),
    mealType: this.formBuilder.control<MealType>('breakfast'),
  });

  protected readonly studyForm = this.formBuilder.group({
    studyProjectId: this.formBuilder.control(''),
    durationMinutes: this.formBuilder.control('', [requiredInteger(1, 1440)]),
    summary: this.formBuilder.control(''),
  });

  /** The day being shown, worded in English from the value the server decided. */
  protected readonly localDateLabel = computed(() =>
    formatEnglishLocalDate(this.summary()?.localDate),
  );

  protected readonly progress = computed(() => this.summary()?.progress ?? null);

  protected readonly routines = computed(() => this.summary()?.routines ?? []);

  protected readonly meals = computed(() => this.summary()?.nutrition.meals ?? []);

  protected readonly studySessions = computed(() => this.summary()?.studySessions ?? []);

  /** Occurrences that carry a time, in chronological order. */
  protected readonly timedItems = computed(() =>
    (this.summary()?.occurrences ?? []).filter((occurrence) => occurrence.startTime !== null),
  );

  /** Occurrences with no time, grouped separately below the timeline. */
  protected readonly untimedItems = computed(() =>
    (this.summary()?.occurrences ?? []).filter((occurrence) => occurrence.startTime === null),
  );

  protected readonly hasAnyPlannedItem = computed(
    () => (this.summary()?.occurrences ?? []).length > 0,
  );

  /**
   * Where the "now" marker belongs in the timeline.
   *
   * The index is the position of the first item that has not started yet. It is `null` on any day
   * that is not the account's current day, because "now" means nothing there. The current time
   * comes from the server, so the marker stays correct even when the device is in another zone.
   */
  protected readonly nowMarkerIndex = computed(() => {
    const current = this.summary();

    if (current === null || !current.isToday) {
      return null;
    }

    const now = current.localTimeOfDay;
    const items = this.timedItems();
    const index = items.findIndex((item) => (item.startTime ?? '') > now);

    return index === -1 ? items.length : index;
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.todayService
      .getSummary()
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (summary) => this.summary.set(summary),
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected openQuickAdd(mode: QuickAddMode): void {
    this.quickAddError.set(null);
    this.quickAddSuccess.set(null);
    this.quickAddMode.update((current) => (current === mode ? 'none' : mode));

    if (this.quickAddMode() === 'study' && this.studyProjects().length === 0) {
      this.loadStudyProjects();
    }
  }

  protected closeQuickAdd(): void {
    this.quickAddMode.set('none');
    this.quickAddError.set(null);
  }

  protected addTask(): void {
    const date = this.summary()?.localDate;

    if (this.isSaving() || date === undefined) {
      return;
    }

    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      this.quickAddError.set('Enter a title for the task.');

      return;
    }

    const value = this.taskForm.getRawValue();

    this.runQuickAdd(
      // Quick add is deliberately the shallow end of the calendar: a one-off task on this day.
      // Kinds, priorities, and repetition live in the calendar's editor, where there is room to
      // explain them.
      this.calendarService.create({
        title: trimValue(value.title),
        description: null,
        kind: 'task',
        category: value.category,
        priority: 'normal',
        startDate: date,
        startTime: trimToNull(value.startTime),
        endTime: null,
        recurrence: {
          frequency: 'none',
          interval: 1,
          endDate: null,
          selectedWeekdays: [],
        },
      }),
      'Task added to today.',
      () => this.taskForm.reset({ title: '', startTime: '', category: value.category }),
    );
  }

  protected addMeal(): void {
    const date = this.summary()?.localDate;

    if (this.isSaving() || date === undefined) {
      return;
    }

    if (this.mealForm.invalid) {
      this.mealForm.markAllAsTouched();
      this.quickAddError.set('Enter what you ate and its calories.');

      return;
    }

    const value = this.mealForm.getRawValue();

    this.runQuickAdd(
      this.nutritionService.createMeal({
        localDate: date,
        mealType: value.mealType,
        name: trimValue(value.name),
        quantity: null,
        calories: parseInteger(value.calories) ?? 0,
        proteinGrams: null,
        carbohydrateGrams: null,
        fatGrams: null,
        occurredAtLocalTime: null,
        notes: null,
      }),
      'Meal recorded.',
      () => this.mealForm.reset({ name: '', calories: '', mealType: value.mealType }),
    );
  }

  protected addStudySession(): void {
    const date = this.summary()?.localDate;

    if (this.isSaving() || date === undefined) {
      return;
    }

    const value = this.studyForm.getRawValue();

    if (trimValue(value.studyProjectId).length === 0) {
      this.quickAddError.set('Choose the project you studied.');

      return;
    }

    if (this.studyForm.controls.durationMinutes.invalid) {
      this.studyForm.controls.durationMinutes.markAsTouched();
      this.quickAddError.set('Enter how many minutes you studied.');

      return;
    }

    this.runQuickAdd(
      this.studyService.createSession({
        studyProjectId: value.studyProjectId,
        localDate: date,
        startTime: null,
        durationMinutes: parseInteger(value.durationMinutes) ?? 0,
        summary: trimToNull(value.summary),
        progressNote: null,
      }),
      'Study session recorded.',
      () =>
        this.studyForm.reset({
          studyProjectId: value.studyProjectId,
          durationMinutes: '',
          summary: '',
        }),
    );
  }

  /**
   * Completes or reopens one occurrence.
   *
   * Only one toggle runs at a time. Without that guard a fast double click would send two requests
   * whose responses could arrive out of order and leave the row showing the wrong state.
   */
  protected toggleCompletion(occurrence: CalendarOccurrence): void {
    this.setStatus(
      occurrence,
      occurrence.status === 'completed' ? 'planned' : 'completed',
    );
  }

  /**
   * Records that something expected today did not happen.
   *
   * Today only ever shows days that have arrived, so the control is offered on any planned item
   * here. The server still applies the same rule, and refuses a future date whatever the client
   * believes.
   */
  protected markFailed(occurrence: CalendarOccurrence): void {
    this.setStatus(occurrence, 'failed');
  }

  protected reopen(occurrence: CalendarOccurrence): void {
    this.setStatus(occurrence, 'planned');
  }

  /** Whether this occurrence is still waiting for an outcome. */
  protected isPlanned(occurrence: CalendarOccurrence): boolean {
    return occurrence.status === 'planned';
  }

  /** Whether a recorded outcome can be undone. */
  protected canReopen(occurrence: CalendarOccurrence): boolean {
    return occurrence.status === 'failed' || occurrence.status === 'cancelled';
  }

  /**
   * Sends one outcome for one occurrence.
   *
   * Only one change runs at a time. Without that guard a fast double click would send two requests
   * whose responses could arrive out of order and leave the row showing the wrong state.
   */
  private setStatus(occurrence: CalendarOccurrence, status: OccurrenceStatus): void {
    if (this.pendingItemId() !== null) {
      return;
    }

    this.pendingItemId.set(occurrence.planningItemId);

    this.calendarService
      .setOccurrenceStatus(occurrence.planningItemId, occurrence.occurrenceDate, status)
      .pipe(
        take(1),
        finalize(() => this.pendingItemId.set(null)),
      )
      .subscribe({
        next: () => this.load(),
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected timeLabel(value: string | null): string {
    return formatTimeLabel(value);
  }

  protected minutesLabel(value: number): string {
    return formatMinutes(value);
  }

  /** Describes an occurrence's state as text, so meaning never depends on colour alone. */
  protected statusLabel(occurrence: CalendarOccurrence): string {
    return statusLabel(occurrence.status);
  }

  private loadStudyProjects(): void {
    this.studyService
      .getProjects()
      .pipe(take(1))
      .subscribe({
        next: (projects) => {
          this.studyProjects.set(projects);

          if (
            projects.length > 0
            && trimValue(this.studyForm.controls.studyProjectId.value).length === 0
          ) {
            this.studyForm.controls.studyProjectId.setValue(projects[0].id);
          }
        },
        error: (error: unknown) => this.quickAddError.set(formLevelMessage(toApiError(error))),
      });
  }

  private runQuickAdd(
    request: Observable<unknown>,
    successMessage: string,
    resetForm: () => void,
  ): void {
    this.isSaving.set(true);
    this.quickAddError.set(null);
    this.quickAddSuccess.set(null);

    request
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          resetForm();
          this.quickAddSuccess.set(successMessage);
          this.load();
        },
        error: (error: unknown) => this.quickAddError.set(formLevelMessage(toApiError(error))),
      });
  }
}
