import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';

import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import {
  optionalInteger,
  parseInteger,
  trimValue,
  trimmedLength,
} from '../../core/forms/validators';
import {
  RECURRENCE_FREQUENCIES,
  ROUTINE_CATEGORIES,
  RecurrenceFrequency,
  RoutineCategory,
  RoutineTemplate,
  describeRecurrence,
} from '../../core/routines/routines.models';
import { RoutinesService } from '../../core/routines/routines.service';
import { TodayService } from '../../core/today/today.service';
import { IsoLocalDate, WEEKDAY_VALUES, Weekday, toIsoLocalDate } from '../../core/time/local-date';

/**
 * The list of routines, with a compact form for creating one.
 *
 * A new routine is created with its header and rule only, then opened so its steps can be built.
 * Splitting it this way keeps the creation form short enough to fill in one breath, which is what
 * a capture screen needs.
 */
@Component({
  selector: 'app-routines',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './routines.component.html',
  styleUrl: './routines.component.scss',
})
export class RoutinesComponent {
  private readonly routinesService = inject(RoutinesService);
  private readonly todayService = inject(TodayService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly frequencies = RECURRENCE_FREQUENCIES;
  protected readonly categories = ROUTINE_CATEGORIES;
  protected readonly weekdays = WEEKDAY_VALUES;

  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSaving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly isFormOpen = signal(false);
  protected readonly routines = signal<readonly RoutineTemplate[]>([]);
  protected readonly selectedWeekdays = signal<readonly Weekday[]>([]);

  /** Start date for a new routine, decided by the server rather than by the browser clock. */
  private readonly todayDate = signal<IsoLocalDate>(toIsoLocalDate(new Date()));

  protected readonly form = this.formBuilder.group({
    name: this.formBuilder.control('', [trimmedLength(1, 150)]),
    description: this.formBuilder.control(''),
    category: this.formBuilder.control<RoutineCategory>('general'),
    frequency: this.formBuilder.control<RecurrenceFrequency>('weekly'),
    interval: this.formBuilder.control('1', [optionalInteger(1, 365)]),
  });

  protected readonly activeRoutines = computed(() =>
    this.routines().filter((routine) => routine.isActive),
  );

  protected readonly inactiveRoutines = computed(() =>
    this.routines().filter((routine) => !routine.isActive),
  );

  protected readonly needsWeekdays = computed(
    () => this.frequencyValue() === 'selectedWeekdays',
  );

  private readonly frequencyValue = signal<RecurrenceFrequency>('weekly');

  constructor() {
    this.form.controls.frequency.valueChanges.subscribe((value) =>
      this.frequencyValue.set(value),
    );

    this.todayService
      .getSummary()
      .pipe(take(1))
      .subscribe({
        next: (summary) => this.todayDate.set(summary.localDate),
        // The browser date is only a fallback for pre-filling a start date the user can change.
        error: () => undefined,
      });

    this.load();
  }

  protected load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.routinesService
      .getTemplates()
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (routines) => this.routines.set(routines),
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected openForm(): void {
    this.formError.set(null);
    this.selectedWeekdays.set([]);
    this.form.reset({
      name: '',
      description: '',
      category: 'general',
      frequency: 'weekly',
      interval: '1',
    });
    this.frequencyValue.set('weekly');
    this.isFormOpen.set(true);
  }

  protected closeForm(): void {
    this.isFormOpen.set(false);
    this.formError.set(null);
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

  protected submit(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set('Enter a name for the routine.');

      return;
    }

    const value = this.form.getRawValue();

    if (value.frequency === 'selectedWeekdays' && this.selectedWeekdays().length === 0) {
      this.formError.set('Choose at least one weekday.');

      return;
    }

    this.isSaving.set(true);

    this.routinesService
      .create({
        name: trimValue(value.name),
        description: trimValue(value.description).length === 0 ? null : trimValue(value.description),
        category: value.category,
        isActive: true,
        steps: [],
        recurrence: {
          frequency: value.frequency,
          interval: parseInteger(value.interval) ?? 1,
          startDate: this.todayDate(),
          endDate: null,
          selectedWeekdays:
            value.frequency === 'selectedWeekdays' ? this.selectedWeekdays() : [],
        },
      })
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: (routine) => {
          this.isFormOpen.set(false);
          // Opening the new routine immediately is what lets the user add its steps next.
          void this.router.navigate(['/app/routines', routine.id]);
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          const firstFieldMessage = Object.values(apiError.validationErrors)[0]?.[0];

          this.formError.set(firstFieldMessage ?? formLevelMessage(apiError));
        },
      });
  }

  protected recurrenceLabel(routine: RoutineTemplate): string {
    return describeRecurrence(routine.recurrence);
  }
}
