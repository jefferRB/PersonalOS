import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import {
  routineOccurrence,
  routineSession,
  routineTemplate,
  todaySummary,
} from '../../../testing/api-fixtures';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import {
  RoutineOccurrence,
  RoutineSession,
  RoutineStep,
  RoutineTemplate,
} from '../../core/routines/routines.models';
import { RoutineDetailComponent } from './routine-detail.component';

describe('RoutineDetailComponent', () => {
  let fixture: ComponentFixture<RoutineDetailComponent>;
  let http: HttpTestingController;

  const benchPress: RoutineStep = {
    id: 'step-1',
    order: 0,
    title: 'Bench press',
    stepType: 'exercise',
    targetSets: 3,
    targetRepetitions: 10,
    targetWeight: 60,
    targetDurationMinutes: null,
    notes: null,
  };

  const inclinePress: RoutineStep = {
    ...benchPress,
    id: 'step-2',
    order: 1,
    title: 'Incline dumbbell press',
    targetWeight: 22.5,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineDetailComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(RoutineDetailComponent);
    fixture.componentRef.setInput('id', '5e5b1d1a-2222-4a2b-9c3d-000000000001');
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('loads the routine and describes its rule in English', () => {
    load(routineTemplate({ steps: [benchPress] }));

    expect(query<HTMLElement>('#routine-title').textContent).toContain('Monday - Chest');
    expect(pageText()).toContain('Every week');
  });

  it('fills the editor with the saved steps', () => {
    load(routineTemplate({ steps: [benchPress, inclinePress] }));

    expect(value('#step-title-0')).toBe('Bench press');
    expect(value('#step-title-1')).toBe('Incline dumbbell press');
    expect(value('#target-weight-0')).toBe('60');
  });

  it('shows exercise fields only for an exercise step', () => {
    load(routineTemplate({ steps: [benchPress] }));

    expect(query<HTMLElement>('#target-sets-0', true)).not.toBeNull();

    select('#step-type-0', 'checklist');

    expect(query<HTMLElement>('#target-sets-0', true)).toBeNull();
  });

  it('adds a step and sends the whole ordered list on save', () => {
    load(routineTemplate({ steps: [benchPress] }));

    clickByText('Add step');
    setValue('#step-title-1', 'Pec deck');
    clickByText('Save routine');
    flushAntiforgery();

    const request = http.expectOne(
      '/api/routines/5e5b1d1a-2222-4a2b-9c3d-000000000001',
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body.steps).toHaveLength(2);
    expect(request.request.body.steps[1]).toMatchObject({ title: 'Pec deck' });

    request.flush(routineTemplate({ steps: [benchPress] }));
    fixture.detectChanges();
    flushOccurrences();
  });

  it('reorders steps with the up and down controls', () => {
    load(routineTemplate({ steps: [benchPress, inclinePress] }));

    const downButtons = queryAll('button').filter((button) =>
      button.textContent?.includes('Move down'),
    );
    downButtons[0].click();
    fixture.detectChanges();

    expect(value('#step-title-0')).toBe('Incline dumbbell press');
    expect(value('#step-title-1')).toBe('Bench press');
  });

  it('disables moving the first step up and the last step down', () => {
    load(routineTemplate({ steps: [benchPress, inclinePress] }));

    const upButtons = queryAll('button').filter((button) =>
      button.textContent?.includes('Move up'),
    ) as HTMLButtonElement[];
    const downButtons = queryAll('button').filter((button) =>
      button.textContent?.includes('Move down'),
    ) as HTMLButtonElement[];

    expect(upButtons[0].disabled).toBe(true);
    expect(downButtons[downButtons.length - 1].disabled).toBe(true);
  });

  it("starts today's session for the day the server decided", () => {
    load(routineTemplate({ steps: [benchPress] }));

    clickByText("Start today's session");
    flushAntiforgery();

    const request = http.expectOne(
      '/api/routines/5e5b1d1a-2222-4a2b-9c3d-000000000001/sessions',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ localDate: '2026-07-30' });

    request.flush(routineSession({ steps: [benchPress] }));
    fixture.detectChanges();

    expect(query<HTMLElement>('.execution', true)).not.toBeNull();
  });

  it('shows the target beside each exercise while recording it', () => {
    load(routineTemplate({ steps: [benchPress] }));
    startSession(routineSession({ steps: [benchPress] }));

    expect(query<HTMLElement>('.execution .chip').textContent).toContain(
      'Target: 3 sets x 10 reps x 60 kg',
    );
  });

  it('records sets, repetitions, and weight, and saves partial progress', () => {
    load(routineTemplate({ steps: [benchPress] }));
    startSession(routineSession({ steps: [benchPress] }));

    check('.execution__check input');
    setValue('#sets-0', '4');
    setValue('#reps-0', '8');
    setValue('#weight-0', '62.5');
    clickByText('Save progress');
    flushAntiforgery();

    const request = http.expectOne('/api/routine-sessions/5e5b1d1a-3333-4a2b-9c3d-000000000001');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body.isCompleted).toBe(false);
    expect(request.request.body.stepResults[0]).toMatchObject({
      routineStepId: 'step-1',
      isCompleted: true,
      actualSets: 4,
      actualRepetitions: 8,
      actualWeight: 62.5,
    });

    request.flush(routineSession({ steps: [benchPress] }));
    fixture.detectChanges();

    expect(pageText()).toContain('Progress saved');
  });

  it('completes the session', () => {
    load(routineTemplate({ steps: [benchPress] }));
    startSession(routineSession({ steps: [benchPress] }));

    clickByText('Complete routine');
    flushAntiforgery();

    const request = http.expectOne('/api/routine-sessions/5e5b1d1a-3333-4a2b-9c3d-000000000001');

    expect(request.request.body.isCompleted).toBe(true);

    request.flush(
      routineSession({ steps: [benchPress], completedAtUtc: '2026-07-30T14:00:00+00:00' }),
    );
    fixture.detectChanges();

    expect(pageText()).toContain('Routine completed');
    expect(query<HTMLElement>('.card__header .chip').textContent).toContain('Completed');
  });

  it('reopens a session that is already recorded, showing what was entered', () => {
    load(routineTemplate({ steps: [benchPress] }), false);
    flushOccurrences(
      [routineOccurrence({ sessionId: '5e5b1d1a-3333-4a2b-9c3d-000000000001' })],
      routineSession({
        steps: [benchPress],
        stepResults: [
          {
            routineStepId: 'step-1',
            isCompleted: true,
            actualSets: 3,
            actualRepetitions: 10,
            actualWeight: 60,
            actualDurationMinutes: null,
            notes: null,
          },
        ],
      }),
    );

    expect(value('#sets-0')).toBe('3');
    expect(value('#weight-0')).toBe('60');
    expect((query<HTMLInputElement>('.execution__check input')).checked).toBe(true);
  });

  it('writes nothing to browser storage', () => {
    load(routineTemplate({ steps: [benchPress] }));

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  /**
   * Answers the start-up requests: the account's current day, the routine itself, and the
   * occurrence query that tells the screen whether a session already exists for today.
   */
  function load(template: RoutineTemplate, withOccurrences = true): void {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    http.expectOne('/api/routines/5e5b1d1a-2222-4a2b-9c3d-000000000001').flush(template);
    fixture.detectChanges();

    if (withOccurrences) {
      flushOccurrences();
    }
  }

  function flushOccurrences(
    occurrences: readonly RoutineOccurrence[] = [],
    session?: RoutineSession,
  ): void {
    for (const request of http.match(
      (candidate) => candidate.url === '/api/routines/occurrences',
    )) {
      request.flush(occurrences);
    }

    fixture.detectChanges();

    if (session !== undefined) {
      http.expectOne(`/api/routine-sessions/${session.id}`).flush(session);
      fixture.detectChanges();
    }
  }

  function startSession(session: RoutineSession): void {
    clickByText("Start today's session");
    flushAntiforgery();
    http
      .expectOne('/api/routines/5e5b1d1a-2222-4a2b-9c3d-000000000001/sessions')
      .flush(session);
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

  function check(selector: string): void {
    const input = query<HTMLInputElement>(selector);
    input.checked = true;
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function setValue(selector: string, newValue: string): void {
    const input = query<HTMLInputElement>(selector);
    input.value = newValue;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function select(selector: string, newValue: string): void {
    const element = query<HTMLSelectElement>(selector);
    element.value = newValue;
    element.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function value(selector: string): string {
    return query<HTMLInputElement>(selector).value;
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
