import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import {
  TEST_LOCAL_DATE,
  nutritionDay,
  nutritionGoal,
  calendarOccurrence,
  routineOccurrence,
  studySession,
  todaySummary,
} from '../../../testing/api-fixtures';
import { CurrentUser } from '../../core/auth/auth.models';
import { AuthStore } from '../../core/auth/auth.store';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { TodaySummary } from '../../core/today/today.models';
import { TodayComponent } from './today.component';

describe('TodayComponent', () => {
  let fixture: ComponentFixture<TodayComponent>;
  let http: HttpTestingController;
  let store: AuthStore;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TodayComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
    store.setAuthenticated(user);

    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(TodayComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('shows an accessible loading state before the day arrives', () => {
    const pending = query<HTMLElement>('.today__date--pending');

    expect(pending.textContent).toContain('Loading your day');
    expect(pending.getAttribute('aria-busy')).toBe('true');
    expect(pending.getAttribute('aria-live')).toBe('polite');

    flush();
  });

  it('asks the API for one aggregated day instead of one request per module', () => {
    const request = http.expectOne('/api/today');

    expect(request.request.method).toBe('GET');

    request.flush(todaySummary());
    fixture.detectChanges();
  });

  it('renders the server local date in English', () => {
    flush();

    expect(pageText()).toContain('Thursday, July 30, 2026');
  });

  it('renders the server date even when it differs from the browser date', () => {
    // The browser's own "today" is irrelevant: the server decided the calendar day.
    flush(todaySummary({ localDate: '1999-12-31' }));

    expect(pageText()).toContain('Friday, December 31, 1999');
  });

  it('does not render a Spanish weekday or month', () => {
    flush();

    const text = pageText();

    expect(text).not.toContain('jueves');
    expect(text).not.toContain('julio');
  });

  it('shows a truthful empty state when nothing is planned', () => {
    flush();

    expect(pageText()).toContain('Nothing is planned for this day yet');
    expect(pageText()).toContain('No routine applies to this day');
  });

  it('renders timed items on the timeline in the order the API returned them', () => {
    flush(
      todaySummary({
        occurrences: [
          calendarOccurrence({ planningItemId: 'a', title: 'Wake up', startTime: '06:00:00' }),
          calendarOccurrence({ planningItemId: 'b', title: 'Train', startTime: '07:00:00' }),
        ],
      }),
    );

    const times = queryAll('.timeline__time').map((element) => element.textContent?.trim());
    const titles = queryAll('.timeline__item .record__title').map((element) =>
      element.textContent?.trim(),
    );

    expect(times).toEqual(['06:00', '07:00']);
    expect(titles).toEqual(['Wake up', 'Train']);
  });

  it('separates untimed items from the timeline', () => {
    flush(
      todaySummary({
        occurrences: [
          calendarOccurrence({ planningItemId: 'a', title: 'Train', startTime: '07:00:00' }),
          calendarOccurrence({ planningItemId: 'b', title: 'Call the bank', startTime: null }),
        ],
      }),
    );

    expect(queryAll('.timeline__item').length).toBe(1);
    expect(query<HTMLElement>('.untimed').textContent).toContain('Call the bank');
  });

  it('states completion as text, not only through colour', () => {
    flush(
      todaySummary({
        occurrences: [
          calendarOccurrence({ planningItemId: 'a', status: 'completed', completedAtUtc: '2026-07-30T13:00:00Z' }),
        ],
      }),
    );

    expect(query<HTMLElement>('.timeline__item .chip').textContent?.trim()).toBe('Completed');
    expect(query<HTMLButtonElement>('.timeline__item button').textContent).toContain('Reopen');
  });

  it('completes an item and reloads the day from the server', () => {
    flush(todaySummary({ occurrences: [calendarOccurrence({ planningItemId: 'item-1' })] }));

    query<HTMLButtonElement>('.timeline__item button').click();

    // Every state-changing request fetches an antiforgery token first.
    flushAntiforgery();

    const completion = http.expectOne(
      '/api/calendar/items/item-1/occurrences/2026-07-30/status',
    );

    expect(completion.request.method).toBe('PUT');

    completion.flush(calendarOccurrence({ planningItemId: 'item-1', status: 'completed' }));
    fixture.detectChanges();

    // The screen re-reads the aggregate so the summary counters cannot drift from the list.
    flush(
      todaySummary({
        occurrences: [calendarOccurrence({ planningItemId: 'item-1', status: 'completed' })],
        progress: { ...todaySummary().progress, plannedItemCount: 1, completedItemCount: 1 },
      }),
    );

    expect(pageText()).toContain('1 of 1 planned items done');
  });

  it('reopens a completed item', () => {
    flush(
      todaySummary({ occurrences: [calendarOccurrence({ planningItemId: 'item-1', status: 'completed' })] }),
    );

    query<HTMLButtonElement>('.timeline__item button').click();
    flushAntiforgery();

    const reopen = http.expectOne(
      '/api/calendar/items/item-1/occurrences/2026-07-30/status',
    );

    expect(reopen.request.method).toBe('PUT');

    reopen.flush(calendarOccurrence({ planningItemId: 'item-1', status: 'planned' }));
    fixture.detectChanges();

    http.expectOne('/api/today').flush(todaySummary());
    fixture.detectChanges();
  });

  it('ignores a second completion click while the first is in flight', () => {
    flush(todaySummary({ occurrences: [calendarOccurrence({ planningItemId: 'item-1' })] }));

    const button = query<HTMLButtonElement>('.timeline__item button');
    button.click();
    fixture.detectChanges();

    expect(button.disabled).toBe(true);

    button.click();

    // Exactly one request exists, so a fast double click cannot produce two out-of-order writes.
    flushAntiforgery();
    http
      .expectOne('/api/calendar/items/item-1/occurrences/2026-07-30/status')
      .flush(calendarOccurrence({ planningItemId: 'item-1' }));
    fixture.detectChanges();
    http.expectOne('/api/today').flush(todaySummary());
    fixture.detectChanges();
  });


  it('offers Mark failed on a planned activity and records the outcome', () => {
    flush(todaySummary({ occurrences: [calendarOccurrence({ planningItemId: 'item-1' })] }));

    const markFailed = [...fixture.nativeElement.querySelectorAll('.timeline__actions button')]
      .find((button: Element) => button.textContent?.includes('Mark failed')) as HTMLButtonElement;

    expect(markFailed).toBeDefined();

    markFailed.click();
    flushAntiforgery();

    const request = http.expectOne(
      '/api/calendar/items/item-1/occurrences/2026-07-30/status',
    );

    // Today uses the calendar's own status command; it has no outcome model of its own.
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ status: 'failed' });

    request.flush(calendarOccurrence({ planningItemId: 'item-1', status: 'failed' }));
    fixture.detectChanges();
    flush(
      todaySummary({
        occurrences: [calendarOccurrence({ planningItemId: 'item-1', status: 'failed' })],
      }),
    );

    expect(pageText()).toContain('Failed');
  });

  it('offers Reopen rather than Complete on a failed activity', () => {
    flush(
      todaySummary({
        occurrences: [calendarOccurrence({ planningItemId: 'item-1', status: 'failed' })],
      }),
    );

    const labels = [...fixture.nativeElement.querySelectorAll('.timeline__actions button')].map(
      (button: Element) => button.textContent?.trim(),
    );

    expect(labels.some((label) => label?.startsWith('Reopen'))).toBe(true);
    expect(labels.some((label) => label?.startsWith('Complete'))).toBe(false);
    expect(labels.some((label) => label?.startsWith('Mark failed'))).toBe(false);
  });

  it('reports only numbers the user actually produced', () => {
    flush(
      todaySummary({
        studySessions: [studySession({ durationMinutes: 90 })],
        nutrition: nutritionDay({
          consumedCalories: 1200,
          goal: nutritionGoal({ dailyCalorieTarget: 2000 }),
          remainingCalories: 800,
        }),
        routines: [routineOccurrence({ isCompleted: true, completedStepCount: 3 })],
        progress: {
          plannedItemCount: 2,
          completedItemCount: 1,
          routineCount: 1,
          completedRoutineCount: 1,
          studyMinutes: 90,
          consumedCalories: 1200,
          dailyCalorieTarget: 2000,
          journalCompleted: true,
        },
      }),
    );

    const text = pageText();

    expect(text).toContain('1/2');
    expect(text).toContain('1200');
    expect(text).toContain('2000');
    expect(text).toContain('1 h 30 min');
    expect(text).toContain('Written');

    // No invented metric appears anywhere on the page.
    expect(text).not.toContain('streak');
    expect(text).not.toContain('score');
  });

  it('states that no calorie target is set instead of inventing one', () => {
    flush(todaySummary());

    expect(pageText()).toContain('No target set');
  });

  it('quick-adds a timed task for the day the server decided', () => {
    flush();

    query<HTMLButtonElement>('.page__actions button').click();
    fixture.detectChanges();

    setValue('#quick-task-title', 'Read a chapter');
    setValue('#quick-task-time', '20:30');

    query<HTMLFormElement>('.quick-add form').dispatchEvent(new Event('submit'));
    flushAntiforgery();

    const request = http.expectOne('/api/calendar/items');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      title: 'Read a chapter',
      kind: 'task',
      startTime: '20:30',
      startDate: TEST_LOCAL_DATE,
    });

    request.flush(calendarOccurrence({ title: 'Read a chapter' }));
    fixture.detectChanges();
    http.expectOne('/api/today').flush(todaySummary());
    fixture.detectChanges();

    expect(query<HTMLElement>('[role="status"]').textContent).toContain('Task added to today');
  });

  it('rejects a whitespace-only task title without calling the API', () => {
    flush();

    query<HTMLButtonElement>('.page__actions button').click();
    fixture.detectChanges();

    setValue('#quick-task-title', '   ');
    query<HTMLFormElement>('.quick-add form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    http.expectNone('/api/calendar/items');
    expect(query<HTMLElement>('[role="alert"]').textContent).toContain('Enter a title');
  });

  it('shows an accessible error state and can retry when the day fails to load', () => {
    http
      .expectOne('/api/today')
      .flush({ title: 'Server error.', status: 500 }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(query<HTMLElement>('[role="alert"]').textContent).toContain(
      'PersonalOS could not complete the request',
    );

    query<HTMLButtonElement>('.alert--error button').click();
    fixture.detectChanges();

    flush();

    expect(query<HTMLElement>('.alert--error', true)).toBeNull();
  });

  it('greets the authenticated user and reacts to a display-name change', () => {
    flush();

    expect(pageText()).toContain('Good day, Jefferson.');

    store.updateDisplayName('Jefferson Rojas');
    fixture.detectChanges();

    expect(pageText()).toContain('Good day, Jefferson Rojas.');
  });

  it('renders a title containing markup as text rather than as HTML', () => {
    flush(
      todaySummary({
        occurrences: [calendarOccurrence({ title: '<img src=x onerror="alert(1)">' })],
      }),
    );

    const title = query<HTMLElement>('.timeline__item .record__title');

    expect(title.querySelector('img')).toBeNull();
    expect(title.textContent).toContain('<img src=x onerror="alert(1)">');
  });

  it('writes nothing from the day to browser storage', () => {
    flush(
      todaySummary({
        occurrences: [calendarOccurrence({ title: 'Private task title' })],
      }),
    );

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
    expect(JSON.stringify({ ...localStorage })).not.toContain('Private task title');
    expect(JSON.stringify({ ...sessionStorage })).not.toContain('Private task title');
  });

  function flush(summary: TodaySummary = todaySummary()): void {
    http.expectOne('/api/today').flush(summary);
    fixture.detectChanges();
  }

  /**
   * Answers the antiforgery request every write makes first.
   *
   * Calling this before each write assertion is itself the test that the client never sends a
   * state-changing request without asking for a token.
   */
  function flushAntiforgery(): void {
    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'test-token' });
    fixture.detectChanges();
  }

  function pageText(): string {
    return fixture.nativeElement.textContent ?? '';
  }

  function setValue(selector: string, value: string): void {
    const input = query<HTMLInputElement>(selector);
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
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
