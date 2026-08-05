import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Observable, finalize, take } from 'rxjs';

import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import {
  optionalNumber,
  parseDecimal,
  parseInteger,
  requiredInteger,
  trimToNull,
  trimValue,
  trimmedLength,
} from '../../core/forms/validators';
import {
  MEAL_TYPES,
  MealEntry,
  MealType,
  NutritionDay,
} from '../../core/nutrition/nutrition.models';
import { NutritionService } from '../../core/nutrition/nutrition.service';
import { IsoLocalDate, formatDayLabel, toInputTime } from '../../core/time/local-date';
import { TodayService } from '../../core/today/today.service';

/** One meal group as rendered on the page. */
interface MealGroup {
  readonly type: MealType;
  readonly label: string;
  readonly meals: readonly MealEntry[];
  readonly calories: number;
}

/**
 * Meals and the daily calorie target.
 *
 * The page reports arithmetic and nothing else. It never labels a value as good or bad, never
 * warns about a deficit or a surplus, and never suggests a target. When consumed calories pass
 * the target, the remaining number simply goes negative and the wording stays factual: deciding
 * what somebody should eat is not this application's job.
 */
@Component({
  selector: 'app-nutrition',
  imports: [ReactiveFormsModule],
  templateUrl: './nutrition.component.html',
  styleUrl: './nutrition.component.scss',
})
export class NutritionComponent {
  private readonly nutritionService = inject(NutritionService);
  private readonly todayService = inject(TodayService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly mealTypes = MEAL_TYPES;

  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly day = signal<NutritionDay | null>(null);
  protected readonly selectedDate = signal<IsoLocalDate>('');

  protected readonly isGoalFormOpen = signal(false);
  protected readonly isMealFormOpen = signal(false);
  protected readonly editingMealId = signal<string | null>(null);
  protected readonly isSaving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly saveSuccess = signal<string | null>(null);
  protected readonly pendingMealId = signal<string | null>(null);

  protected readonly goalForm = this.formBuilder.group({
    dailyCalorieTarget: this.formBuilder.control('', [requiredInteger(500, 20000)]),
    proteinTargetGrams: this.formBuilder.control('', [optionalNumber(0, 2000)]),
    carbohydrateTargetGrams: this.formBuilder.control('', [optionalNumber(0, 2000)]),
    fatTargetGrams: this.formBuilder.control('', [optionalNumber(0, 2000)]),
  });

  protected readonly mealForm = this.formBuilder.group({
    name: this.formBuilder.control('', [trimmedLength(1, 200)]),
    quantity: this.formBuilder.control(''),
    mealType: this.formBuilder.control<MealType>('breakfast'),
    calories: this.formBuilder.control('', [requiredInteger(0, 20000)]),
    proteinGrams: this.formBuilder.control('', [optionalNumber(0, 2000)]),
    carbohydrateGrams: this.formBuilder.control('', [optionalNumber(0, 2000)]),
    fatGrams: this.formBuilder.control('', [optionalNumber(0, 2000)]),
    occurredAtLocalTime: this.formBuilder.control(''),
    notes: this.formBuilder.control(''),
  });

  protected readonly dayLabel = computed(() => formatDayLabel(this.selectedDate()));

  protected readonly hasTarget = computed(
    () => this.day()?.goal.dailyCalorieTarget !== null && this.day() !== null,
  );

  /** Meals grouped by meal type, in the order a day is eaten. */
  protected readonly mealGroups = computed<readonly MealGroup[]>(() => {
    const meals = this.day()?.meals ?? [];

    return MEAL_TYPES.map((type) => {
      const groupMeals = meals.filter((meal) => meal.mealType === type.value);

      return {
        type: type.value,
        label: type.label,
        meals: groupMeals,
        calories: groupMeals.reduce((total, meal) => total + meal.calories, 0),
      };
    }).filter((group) => group.meals.length > 0);
  });

  constructor() {
    this.todayService
      .getSummary()
      .pipe(take(1))
      .subscribe({
        next: (summary) => {
          this.selectedDate.set(summary.localDate);
          this.load();
        },
        error: (error: unknown) => {
          this.loadError.set(formLevelMessage(toApiError(error)));
          this.isLoading.set(false);
        },
      });
  }

  protected load(): void {
    const date = this.selectedDate();

    if (date.length === 0) {
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);

    this.nutritionService
      .getDay(date)
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (day) => this.day.set(day),
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected onDateChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;

    if (value.length > 0) {
      this.selectedDate.set(value);
      this.saveSuccess.set(null);
      this.load();
    }
  }

  protected openGoalForm(): void {
    const goal = this.day()?.goal;

    this.formError.set(null);
    this.saveSuccess.set(null);
    this.goalForm.reset({
      dailyCalorieTarget: numberText(goal?.dailyCalorieTarget),
      proteinTargetGrams: numberText(goal?.proteinTargetGrams),
      carbohydrateTargetGrams: numberText(goal?.carbohydrateTargetGrams),
      fatTargetGrams: numberText(goal?.fatTargetGrams),
    });
    this.isGoalFormOpen.set(true);
  }

  protected closeGoalForm(): void {
    this.isGoalFormOpen.set(false);
    this.formError.set(null);
  }

  protected saveGoal(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);

    if (this.goalForm.invalid) {
      this.goalForm.markAllAsTouched();
      this.formError.set('Enter a whole number of calories between 500 and 20000.');

      return;
    }

    const value = this.goalForm.getRawValue();

    this.runSave(
      this.nutritionService.saveGoal({
        dailyCalorieTarget: parseInteger(value.dailyCalorieTarget) ?? 0,
        proteinTargetGrams: parseDecimal(value.proteinTargetGrams),
        carbohydrateTargetGrams: parseDecimal(value.carbohydrateTargetGrams),
        fatTargetGrams: parseDecimal(value.fatTargetGrams),
      }),
      'Daily target saved.',
      () => this.isGoalFormOpen.set(false),
    );
  }

  protected openMealForm(meal?: MealEntry): void {
    this.editingMealId.set(meal?.id ?? null);
    this.formError.set(null);
    this.saveSuccess.set(null);
    this.mealForm.reset({
      name: meal?.name ?? '',
      quantity: meal?.quantity ?? '',
      mealType: meal?.mealType ?? 'breakfast',
      calories: numberText(meal?.calories),
      proteinGrams: numberText(meal?.proteinGrams),
      carbohydrateGrams: numberText(meal?.carbohydrateGrams),
      fatGrams: numberText(meal?.fatGrams),
      occurredAtLocalTime: toInputTime(meal?.occurredAtLocalTime),
      notes: meal?.notes ?? '',
    });
    this.isMealFormOpen.set(true);
  }

  protected closeMealForm(): void {
    this.isMealFormOpen.set(false);
    this.formError.set(null);
  }

  protected saveMeal(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);

    if (this.mealForm.invalid) {
      this.mealForm.markAllAsTouched();
      this.formError.set('Enter what you ate and a whole number of calories.');

      return;
    }

    const value = this.mealForm.getRawValue();
    const request = {
      localDate: this.selectedDate(),
      mealType: value.mealType,
      name: trimValue(value.name),
      quantity: trimToNull(value.quantity),
      calories: parseInteger(value.calories) ?? 0,
      proteinGrams: parseDecimal(value.proteinGrams),
      carbohydrateGrams: parseDecimal(value.carbohydrateGrams),
      fatGrams: parseDecimal(value.fatGrams),
      occurredAtLocalTime: trimToNull(value.occurredAtLocalTime),
      notes: trimToNull(value.notes),
    };

    const editingId = this.editingMealId();

    this.runSave(
      editingId === null
        ? this.nutritionService.createMeal(request)
        : this.nutritionService.updateMeal(editingId, request),
      editingId === null ? 'Meal recorded.' : 'Meal updated.',
      () => this.isMealFormOpen.set(false),
    );
  }

  protected deleteMeal(meal: MealEntry): void {
    if (this.pendingMealId() !== null) {
      return;
    }

    if (!window.confirm(`Delete "${meal.name}"? This cannot be undone.`)) {
      return;
    }

    this.pendingMealId.set(meal.id);

    this.nutritionService
      .deleteMeal(meal.id)
      .pipe(
        take(1),
        finalize(() => this.pendingMealId.set(null)),
      )
      .subscribe({
        next: () => {
          this.saveSuccess.set('Meal deleted.');
          this.load();
        },
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected timeLabel(value: string | null): string {
    return toInputTime(value);
  }

  private runSave(
    request: Observable<unknown>,
    successMessage: string,
    onSuccess: () => void,
  ): void {
    this.isSaving.set(true);

    request
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          onSuccess();
          this.saveSuccess.set(successMessage);
          this.load();
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          const firstFieldMessage = Object.values(apiError.validationErrors)[0]?.[0];

          this.formError.set(firstFieldMessage ?? formLevelMessage(apiError));
        },
      });
  }
}

/** Renders an optional number as the text a control expects, with `null` becoming empty. */
function numberText(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}
