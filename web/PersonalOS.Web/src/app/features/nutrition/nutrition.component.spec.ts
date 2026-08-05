import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { nutritionDay, nutritionGoal, todaySummary } from '../../../testing/api-fixtures';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { MealEntry, NutritionDay } from '../../core/nutrition/nutrition.models';
import { NutritionComponent } from './nutrition.component';

describe('NutritionComponent', () => {
  let fixture: ComponentFixture<NutritionComponent>;
  let http: HttpTestingController;

  const breakfast: MealEntry = {
    id: 'meal-1',
    localDate: '2026-07-30',
    mealType: 'breakfast',
    name: 'Oats and banana',
    quantity: '80 g',
    calories: 420,
    proteinGrams: 12,
    carbohydrateGrams: 70,
    fatGrams: 8,
    occurredAtLocalTime: '07:15:00',
    notes: null,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NutritionComponent],
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(NutritionComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('loads the day the server decided rather than the browser date', () => {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    const request = http.expectOne(
      (candidate) => candidate.url === '/api/nutrition/day',
    );

    expect(request.request.params.get('date')).toBe('2026-07-30');

    request.flush(nutritionDay());
    fixture.detectChanges();
  });

  it('states plainly that no target is set instead of proposing one', () => {
    load(nutritionDay());

    expect(pageText()).toContain('Not set');
    expect(pageText()).toContain('Set a target to see the difference');
  });

  it('shows consumed, target, and remaining calories', () => {
    load(
      nutritionDay({
        goal: nutritionGoal({ dailyCalorieTarget: 2000 }),
        consumedCalories: 1400,
        remainingCalories: 600,
        meals: [breakfast],
      }),
    );

    const text = pageText();

    expect(text).toContain('1400');
    expect(text).toContain('2000');
    expect(text).toContain('600');
    expect(text).toContain('Calories below your target');
  });

  it('reports going over the target as a factual number, with neutral wording', () => {
    load(
      nutritionDay({
        goal: nutritionGoal({ dailyCalorieTarget: 2000 }),
        consumedCalories: 2400,
        remainingCalories: -400,
      }),
    );

    const text = pageText();

    expect(text).toContain('-400');
    expect(text).toContain('Calories above your target');

    // The page must never scold, diagnose, or advise.
    for (const forbidden of ['too many', 'unhealthy', 'should eat', 'warning', 'exceeded limit']) {
      expect(text.toLowerCase()).not.toContain(forbidden);
    }
  });

  it('groups meals by meal type with a per-group total', () => {
    load(
      nutritionDay({
        meals: [
          breakfast,
          { ...breakfast, id: 'meal-2', mealType: 'lunch', name: 'Rice and chicken', calories: 700 },
        ],
        consumedCalories: 1120,
      }),
    );

    const headings = queryAll('.meal-group h3').map((element) => element.textContent?.trim());

    expect(headings[0]).toContain('Breakfast');
    expect(headings[0]).toContain('420 kcal');
    expect(headings[1]).toContain('Lunch');
    expect(headings[1]).toContain('700 kcal');
  });

  it('saves a daily calorie target', () => {
    load(nutritionDay());

    clickByText('Set target');
    setValue('#goal-calories', '2200');
    submit('.inline-form');
    flushAntiforgery();

    const request = http.expectOne('/api/nutrition/goal');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toMatchObject({ dailyCalorieTarget: 2200 });

    request.flush(nutritionGoal({ dailyCalorieTarget: 2200 }));
    fixture.detectChanges();
    load(nutritionDay({ goal: nutritionGoal({ dailyCalorieTarget: 2200 }) }));

    expect(pageText()).toContain('Daily target saved');
  });

  it('rejects a calorie target outside the stored range without calling the API', () => {
    load(nutritionDay());

    clickByText('Set target');
    setValue('#goal-calories', '10');
    submit('.inline-form');

    http.expectNone('/api/nutrition/goal');
    expect(query<HTMLElement>('[role="alert"]').textContent).toContain('between 500 and 20000');
  });

  it('records a meal with its calories', () => {
    load(nutritionDay());

    clickByText('Add meal');
    setValue('#meal-name', 'Oats');
    setValue('#meal-calories', '420');
    submit('.inline-form');
    flushAntiforgery();

    const request = http.expectOne('/api/meals');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      name: 'Oats',
      calories: 420,
      localDate: '2026-07-30',
    });

    request.flush(breakfast);
    fixture.detectChanges();
    load(nutritionDay({ meals: [breakfast], consumedCalories: 420 }));

    expect(pageText()).toContain('Meal recorded');
  });

  it('rejects a whitespace-only meal name without calling the API', () => {
    load(nutritionDay());

    clickByText('Add meal');
    setValue('#meal-name', '   ');
    setValue('#meal-calories', '420');
    submit('.inline-form');

    http.expectNone('/api/meals');
    expect(query<HTMLElement>('[role="alert"]').textContent).toContain('Enter what you ate');
  });

  it('renders a meal name containing markup as text', () => {
    load(nutritionDay({ meals: [{ ...breakfast, name: '<b>bold</b>' }] }));

    const title = query<HTMLElement>('.record__title');

    expect(title.querySelector('b')).toBeNull();
    expect(title.textContent).toContain('<b>bold</b>');
  });

  it('writes nothing to browser storage', () => {
    load(nutritionDay({ meals: [breakfast] }));

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  function load(day: NutritionDay): void {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    for (const request of http.match((candidate) => candidate.url === '/api/nutrition/day')) {
      request.flush(day);
    }

    fixture.detectChanges();
  }

  function flushAntiforgery(): void {
    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'test-token' });
    fixture.detectChanges();
  }

  function clickByText(text: string): void {
    const button = queryAll('button').find((candidate) => candidate.textContent?.includes(text));

    expect(button).not.toBeUndefined();
    button?.click();
    fixture.detectChanges();
  }

  function submit(selector: string): void {
    query<HTMLFormElement>(selector).dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function setValue(selector: string, value: string): void {
    const input = query<HTMLInputElement>(selector);
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function pageText(): string {
    return fixture.nativeElement.textContent ?? '';
  }

  function query<T extends HTMLElement>(selector: string, allowMissing = false): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;

    if (!allowMissing) {
      expect(element).not.toBeNull();
    }

    return element as T;
  }

  function queryAll(selector: string): HTMLElement[] {
    return [...(fixture.nativeElement.querySelectorAll(selector) as NodeListOf<HTMLElement>)];
  }
});
