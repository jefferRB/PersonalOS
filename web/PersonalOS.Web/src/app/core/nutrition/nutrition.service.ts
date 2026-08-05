import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AntiforgeryService } from '../auth/antiforgery.service';
import { IsoLocalDate } from '../time/local-date';
import {
  MealEntry,
  NutritionDay,
  NutritionGoal,
  SaveMealRequest,
  SaveNutritionGoalRequest,
} from './nutrition.models';

/**
 * Meals and daily calorie totals of the authenticated account.
 *
 * The service moves numbers the user typed. It contacts no nutrition database and derives no
 * recommendation.
 */
@Injectable({ providedIn: 'root' })
export class NutritionService {
  private readonly http = inject(HttpClient);
  private readonly antiforgery = inject(AntiforgeryService);

  getDay(date: IsoLocalDate): Observable<NutritionDay> {
    return this.http.get<NutritionDay>('/api/nutrition/day', { params: { date } });
  }

  getGoal(): Observable<NutritionGoal> {
    return this.http.get<NutritionGoal>('/api/nutrition/goal');
  }

  saveGoal(request: SaveNutritionGoalRequest): Observable<NutritionGoal> {
    return this.antiforgery.protect(() =>
      this.http.put<NutritionGoal>('/api/nutrition/goal', request),
    );
  }

  createMeal(request: SaveMealRequest): Observable<MealEntry> {
    return this.antiforgery.protect(() => this.http.post<MealEntry>('/api/meals', request));
  }

  updateMeal(id: string, request: SaveMealRequest): Observable<MealEntry> {
    return this.antiforgery.protect(() =>
      this.http.put<MealEntry>(`/api/meals/${id}`, request),
    );
  }

  deleteMeal(id: string): Observable<void> {
    return this.antiforgery.protect(() => this.http.delete<void>(`/api/meals/${id}`));
  }
}
