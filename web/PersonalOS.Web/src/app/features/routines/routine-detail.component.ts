import { Component, computed, inject, input, signal } from '@angular/core';
import { FormArray, FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';

import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import {
  optionalInteger,
  optionalNumber,
  parseDecimal,
  parseInteger,
  trimToNull,
  trimValue,
  trimmedLength,
} from '../../core/forms/validators';
import {
  RECURRENCE_FREQUENCIES,
  ROUTINE_CATEGORIES,
  ROUTINE_STEP_TYPES,
  RecurrenceFrequency,
  RoutineCategory,
  RoutineSession,
  RoutineStep,
  RoutineStepResult,
  RoutineStepType,
  RoutineTemplate,
  describeRecurrence,
} from '../../core/routines/routines.models';
import { RoutinesService } from '../../core/routines/routines.service';
import { IsoLocalDate, WEEKDAY_VALUES, Weekday, toIsoLocalDate } from '../../core/time/local-date';
import { TodayService } from '../../core/today/today.service';

/** One step row inside the editor form. */
type StepGroupControls = {
  title: FormControl<string>;
  stepType: FormControl<RoutineStepType>;
  targetSets: FormControl<string>;
  targetRepetitions: FormControl<string>;
  targetWeight: FormControl<string>;
  targetDurationMinutes: FormControl<string>;
  notes: FormControl<string>;
};

/** One step row inside the execution form. */
type ResultGroupControls = {
  routineStepId: FormControl<string>;
  isCompleted: FormControl<boolean>;
  actualSets: FormControl<string>;
  actualRepetitions: FormControl<string>;
  actualWeight: FormControl<string>;
  actualDurationMinutes: FormControl<string>;
  notes: FormControl<string>;
};

/**
 * One routine: its editor and the screen used to execute it today.
 *
 * The two halves are on one page because a workout is edited and performed in the same breath:
 * you notice that a target is wrong while you are lifting.
 *
 * The editor sends the whole step list on every save. Reconciling added, moved, and removed steps
 * from partial instructions would need an identifier scheme the client could get wrong; sending
 * the list the user sees, and letting the server renumber it, cannot produce a duplicate position.
 */
@Component({
  selector: 'app-routine-detail',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './routine-detail.component.html',
  styleUrl: './routine-detail.component.scss',
})
export class RoutineDetailComponent {
  /** Routine identifier, bound from the route parameter. */
  readonly id = input.required<string>();

  private readonly routinesService = inject(RoutinesService);
  private readonly todayService = inject(TodayService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly frequencies = RECURRENCE_FREQUENCIES;
  protected readonly categories = ROUTINE_CATEGORIES;
  protected readonly stepTypes = ROUTINE_STEP_TYPES;
  protected readonly weekdays = WEEKDAY_VALUES;

  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSaving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly saveSuccess = signal<string | null>(null);

  protected readonly routine = signal<RoutineTemplate | null>(null);
  protected readonly session = signal<RoutineSession | null>(null);
  protected readonly isSessionSaving = signal(false);
  protected readonly sessionError = signal<string | null>(null);
  protected readonly sessionSuccess = signal<string | null>(null);
  protected readonly selectedWeekdays = signal<readonly Weekday[]>([]);

  private readonly todayDate = signal<IsoLocalDate>(toIsoLocalDate(new Date()));

  protected readonly form = this.formBuilder.group({
    name: this.formBuilder.control('', [trimmedLength(1, 150)]),
    description: this.formBuilder.control(''),
    category: this.formBuilder.control<RoutineCategory>('general'),
    isActive: this.formBuilder.control(true),
    frequency: this.formBuilder.control<RecurrenceFrequency>('weekly'),
    interval: this.formBuilder.control('1', [optionalInteger(1, 365)]),
    startDate: this.formBuilder.control(''),
    endDate: this.formBuilder.control(''),
    steps: this.formBuilder.array<ReturnType<RoutineDetailComponent['createStepGroup']>>([]),
  });

  protected readonly sessionForm = this.formBuilder.group({
    notes: this.formBuilder.control(''),
    results: this.formBuilder.array<ReturnType<RoutineDetailComponent['createResultGroup']>>([]),
  });

  protected readonly steps = computed(() => this.form.controls.steps);

  protected readonly needsWeekdays = computed(
    () => this.frequencyValue() === 'selectedWeekdays',
  );

  protected readonly recurrenceLabel = computed(() => {
    const current = this.routine();

    return current === null ? '' : describeRecurrence(current.recurrence);
  });

  /** Whether this routine applies to the account's current local day. */
  protected readonly appliesToday = computed(() =>
    this.occurrenceDates().includes(this.todayDate()),
  );

  private readonly occurrenceDates = signal<readonly IsoLocalDate[]>([]);
  private readonly frequencyValue = signal<RecurrenceFrequency>('weekly');

  constructor() {
    this.form.controls.frequency.valueChanges.subscribe((value) =>
      this.frequencyValue.set(value),
    );

    this.todayService
      .getSummary()
      .pipe(take(1))
      .subscribe({
        next: (summary) => {
          this.todayDate.set(summary.localDate);
          this.load();
        },
        error: () => this.load(),
      });
  }

  protected load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.routinesService
      .getTemplate(this.id())
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (routine) => {
          this.applyRoutine(routine);
          this.loadOccurrence();
        },
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected get stepControls(): FormArray<ReturnType<RoutineDetailComponent['createStepGroup']>> {
    return this.form.controls.steps;
  }

  protected get resultControls(): FormArray<
    ReturnType<RoutineDetailComponent['createResultGroup']>
  > {
    return this.sessionForm.controls.results;
  }

  protected addStep(): void {
    this.stepControls.push(this.createStepGroup());
    this.saveSuccess.set(null);
  }

  protected removeStep(index: number): void {
    this.stepControls.removeAt(index);
    this.saveSuccess.set(null);
  }

  /**
   * Moves a step one position up or down.
   *
   * Up and down buttons are used instead of drag and drop: they work with a keyboard, they need
   * no dependency, and they are usable on a phone at the gym.
   */
  protected moveStep(index: number, offset: number): void {
    const target = index + offset;

    if (target < 0 || target >= this.stepControls.length) {
      return;
    }

    const control = this.stepControls.at(index);
    this.stepControls.removeAt(index);
    this.stepControls.insert(target, control);
    this.saveSuccess.set(null);
  }

  protected isExerciseStep(index: number): boolean {
    return this.stepControls.at(index).controls.stepType.value === 'exercise';
  }

  protected isTimedStep(index: number): boolean {
    return this.stepControls.at(index).controls.stepType.value === 'timed';
  }

  protected toggleWeekday(weekday: Weekday): void {
    this.selectedWeekdays.update((current) =>
      current.includes(weekday)
        ? current.filter((value) => value !== weekday)
        : [...current, weekday],
    );
  }

  protected isWeekdaySelected(weekday: Weekday): boolean {
    return this.selectedWeekdays().includes(weekday);
  }

  protected save(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);
    this.saveSuccess.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Review the highlighted fields and try again.');

      return;
    }

    const value = this.form.getRawValue();

    if (value.frequency === 'selectedWeekdays' && this.selectedWeekdays().length === 0) {
      this.formError.set('Choose at least one weekday.');

      return;
    }

    this.isSaving.set(true);

    this.routinesService
      .update(this.id(), {
        name: trimValue(value.name),
        description: trimToNull(value.description),
        category: value.category,
        isActive: value.isActive,
        recurrence: {
          frequency: value.frequency,
          interval: parseInteger(value.interval) ?? 1,
          startDate: trimValue(value.startDate) || this.todayDate(),
          endDate: trimToNull(value.endDate),
          selectedWeekdays:
            value.frequency === 'selectedWeekdays' ? this.selectedWeekdays() : [],
        },
        steps: value.steps.map((step) => ({
          title: trimValue(step.title),
          stepType: step.stepType,
          targetSets: step.stepType === 'exercise' ? parseInteger(step.targetSets) : null,
          targetRepetitions:
            step.stepType === 'exercise' ? parseInteger(step.targetRepetitions) : null,
          targetWeight: step.stepType === 'exercise' ? parseDecimal(step.targetWeight) : null,
          targetDurationMinutes:
            step.stepType === 'timed' ? parseInteger(step.targetDurationMinutes) : null,
          notes: trimToNull(step.notes),
        })),
      })
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: (routine) => {
          this.applyRoutine(routine);
          this.saveSuccess.set('Routine saved.');
          this.loadOccurrence();
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          const firstFieldMessage = Object.values(apiError.validationErrors)[0]?.[0];

          this.formError.set(firstFieldMessage ?? formLevelMessage(apiError));
        },
      });
  }

  protected startSession(): void {
    if (this.isSessionSaving()) {
      return;
    }

    this.isSessionSaving.set(true);
    this.sessionError.set(null);

    this.routinesService
      .startSession(this.id(), this.todayDate())
      .pipe(
        take(1),
        finalize(() => this.isSessionSaving.set(false)),
      )
      .subscribe({
        next: (session) => this.applySession(session),
        error: (error: unknown) => this.sessionError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected saveSession(complete: boolean): void {
    const current = this.session();

    if (current === null || this.isSessionSaving()) {
      return;
    }

    this.isSessionSaving.set(true);
    this.sessionError.set(null);
    this.sessionSuccess.set(null);

    const value = this.sessionForm.getRawValue();

    this.routinesService
      .saveSession(current.id, {
        notes: trimToNull(value.notes),
        isCompleted: complete,
        stepResults: value.results.map((result) => ({
          routineStepId: result.routineStepId,
          isCompleted: result.isCompleted,
          actualSets: parseInteger(result.actualSets),
          actualRepetitions: parseInteger(result.actualRepetitions),
          actualWeight: parseDecimal(result.actualWeight),
          actualDurationMinutes: parseInteger(result.actualDurationMinutes),
          notes: trimToNull(result.notes),
        })),
      })
      .pipe(
        take(1),
        finalize(() => this.isSessionSaving.set(false)),
      )
      .subscribe({
        next: (session) => {
          this.applySession(session);
          this.sessionSuccess.set(
            complete ? 'Routine completed.' : 'Progress saved. You can finish it later.',
          );
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          const firstFieldMessage = Object.values(apiError.validationErrors)[0]?.[0];

          this.sessionError.set(firstFieldMessage ?? formLevelMessage(apiError));
        },
      });
  }

  /** The saved target step matching one execution row, so targets show beside results. */
  protected stepForResult(index: number): RoutineStep | null {
    const stepId = this.resultControls.at(index).controls.routineStepId.value;

    return this.session()?.steps.find((step) => step.id === stepId) ?? null;
  }

  protected targetSummary(step: RoutineStep | null): string {
    if (step === null) {
      return '';
    }

    if (step.stepType === 'exercise') {
      const parts: string[] = [];

      if (step.targetSets !== null) {
        parts.push(`${step.targetSets} sets`);
      }

      if (step.targetRepetitions !== null) {
        parts.push(`${step.targetRepetitions} reps`);
      }

      if (step.targetWeight !== null) {
        parts.push(`${step.targetWeight} kg`);
      }

      return parts.length === 0 ? 'No target set' : `Target: ${parts.join(' x ')}`;
    }

    if (step.stepType === 'timed' && step.targetDurationMinutes !== null) {
      return `Target: ${step.targetDurationMinutes} min`;
    }

    return '';
  }

  protected isExerciseResult(index: number): boolean {
    return this.stepForResult(index)?.stepType === 'exercise';
  }

  protected isTimedResult(index: number): boolean {
    return this.stepForResult(index)?.stepType === 'timed';
  }

  private applyRoutine(routine: RoutineTemplate): void {
    this.routine.set(routine);
    this.selectedWeekdays.set([...routine.recurrence.selectedWeekdays]);
    this.frequencyValue.set(routine.recurrence.frequency);

    this.stepControls.clear();

    for (const step of routine.steps) {
      this.stepControls.push(this.createStepGroup(step));
    }

    this.form.patchValue(
      {
        name: routine.name,
        description: routine.description ?? '',
        category: routine.category,
        isActive: routine.isActive,
        frequency: routine.recurrence.frequency,
        interval: String(routine.recurrence.interval),
        startDate: routine.recurrence.startDate,
        endDate: routine.recurrence.endDate ?? '',
      },
      { emitEvent: false },
    );
    this.form.markAsPristine();
  }

  private applySession(session: RoutineSession): void {
    this.session.set(session);
    this.resultControls.clear();

    for (const step of session.steps) {
      const result = session.stepResults.find((item) => item.routineStepId === step.id);
      this.resultControls.push(this.createResultGroup(step, result));
    }

    this.sessionForm.controls.notes.setValue(session.notes ?? '');
  }

  /**
   * Loads whether the routine applies today, and any session already recorded for it.
   *
   * Only the current day is requested, so the calculation stays bounded no matter how far the
   * recurrence reaches.
   */
  private loadOccurrence(): void {
    const date = this.todayDate();

    this.routinesService
      .getOccurrences(date, date)
      .pipe(take(1))
      .subscribe({
        next: (occurrences) => {
          const mine = occurrences.filter(
            (occurrence) => occurrence.routineTemplateId === this.id(),
          );

          this.occurrenceDates.set(mine.map((occurrence) => occurrence.localDate));

          const sessionId = mine[0]?.sessionId ?? null;

          if (sessionId !== null) {
            this.routinesService
              .getSession(sessionId)
              .pipe(take(1))
              .subscribe({
                next: (session) => this.applySession(session),
                error: (error: unknown) =>
                  this.sessionError.set(formLevelMessage(toApiError(error))),
              });
          }
        },
        error: (error: unknown) => this.sessionError.set(formLevelMessage(toApiError(error))),
      });
  }

  private createStepGroup(step?: RoutineStep) {
    return this.formBuilder.group<StepGroupControls>({
      title: this.formBuilder.control(step?.title ?? '', [trimmedLength(1, 200)]),
      stepType: this.formBuilder.control<RoutineStepType>(step?.stepType ?? 'checklist'),
      targetSets: this.formBuilder.control(numberText(step?.targetSets), [
        optionalInteger(1, 1000),
      ]),
      targetRepetitions: this.formBuilder.control(numberText(step?.targetRepetitions), [
        optionalInteger(1, 1000),
      ]),
      targetWeight: this.formBuilder.control(numberText(step?.targetWeight), [
        optionalNumber(0, 2000),
      ]),
      targetDurationMinutes: this.formBuilder.control(numberText(step?.targetDurationMinutes), [
        optionalInteger(1, 1440),
      ]),
      notes: this.formBuilder.control(step?.notes ?? ''),
    });
  }

  private createResultGroup(step: RoutineStep, result?: RoutineStepResult) {
    return this.formBuilder.group<ResultGroupControls>({
      routineStepId: this.formBuilder.control(step.id),
      isCompleted: this.formBuilder.control(result?.isCompleted ?? false),
      actualSets: this.formBuilder.control(numberText(result?.actualSets), [
        optionalInteger(1, 1000),
      ]),
      actualRepetitions: this.formBuilder.control(numberText(result?.actualRepetitions), [
        optionalInteger(1, 1000),
      ]),
      actualWeight: this.formBuilder.control(numberText(result?.actualWeight), [
        optionalNumber(0, 2000),
      ]),
      actualDurationMinutes: this.formBuilder.control(
        numberText(result?.actualDurationMinutes),
        [optionalInteger(1, 1440)],
      ),
      notes: this.formBuilder.control(result?.notes ?? ''),
    });
  }
}

/** Renders an optional number as the text a control expects, with `null` becoming empty. */
function numberText(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}
