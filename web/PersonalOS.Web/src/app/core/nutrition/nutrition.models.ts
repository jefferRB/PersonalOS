import { IsoLocalDate } from '../time/local-date';

/** Which meal of the day, exactly as the API spells it. */
export type MealType = 'breakfast' | 'lunch' | 'dinner' | 'snack' | 'other';

/** Meal groups, in the order a day is eaten. */
export const MEAL_TYPES: readonly { value: MealType; label: string }[] = [
  { value: 'breakfast', label: 'Breakfast' },
  { value: 'lunch', label: 'Lunch' },
  { value: 'dinner', label: 'Dinner' },
  { value: 'snack', label: 'Snack' },
  { value: 'other', label: 'Other' },
];

/** The daily targets of the authenticated account. */
export interface NutritionGoal {
  readonly dailyCalorieTarget: number | null;
  readonly proteinTargetGrams: number | null;
  readonly carbohydrateTargetGrams: number | null;
  readonly fatTargetGrams: number | null;
  readonly updatedAtUtc: string | null;
}

/** One recorded meal. */
export interface MealEntry {
  readonly id: string;
  readonly localDate: IsoLocalDate;
  readonly mealType: MealType;
  readonly name: string;
  readonly quantity: string | null;
  readonly calories: number;
  readonly proteinGrams: number | null;
  readonly carbohydrateGrams: number | null;
  readonly fatGrams: number | null;
  readonly occurredAtLocalTime: string | null;
  readonly notes: string | null;
}

/**
 * What was eaten on one local day, beside the target the user chose.
 *
 * `remainingCalories` goes negative once the target is passed. The interface shows that number as
 * a fact and never as a warning: PersonalOS does not judge what somebody ate.
 */
export interface NutritionDay {
  readonly localDate: IsoLocalDate;
  readonly goal: NutritionGoal;
  readonly consumedCalories: number;
  readonly remainingCalories: number | null;
  readonly proteinGrams: number;
  readonly carbohydrateGrams: number;
  readonly fatGrams: number;
  readonly meals: readonly MealEntry[];
}

/** Values sent when saving the daily targets. */
export interface SaveNutritionGoalRequest {
  readonly dailyCalorieTarget: number;
  readonly proteinTargetGrams: number | null;
  readonly carbohydrateTargetGrams: number | null;
  readonly fatTargetGrams: number | null;
}

/** Values sent when creating or editing a meal. */
export interface SaveMealRequest {
  readonly localDate: IsoLocalDate;
  readonly mealType: MealType;
  readonly name: string;
  readonly quantity: string | null;
  readonly calories: number;
  readonly proteinGrams: number | null;
  readonly carbohydrateGrams: number | null;
  readonly fatGrams: number | null;
  readonly occurredAtLocalTime: string | null;
  readonly notes: string | null;
}
