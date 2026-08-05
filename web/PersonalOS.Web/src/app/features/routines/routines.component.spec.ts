import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { routineTemplate, todaySummary } from '../../../testing/api-fixtures';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { RoutineTemplate } from '../../core/routines/routines.models';
import { RoutinesComponent } from './routines.component';

describe('RoutinesComponent', () => {
  let fixture: ComponentFixture<RoutinesComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutinesComponent],
      providers: [
        // Creating a routine navigates straight to its detail page so the steps can be added, so
        // the route has to exist for the navigation to resolve.
        provideRouter([{ path: 'app/routines/:id', children: [] }]),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(RoutinesComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('shows a truthful empty state before any routine exists', () => {
    load([]);

    expect(pageText()).toContain('You have no routines yet');
  });

  it('separates active routines from inactive ones and summarises each rule', () => {
    load([
      routineTemplate({ id: 'a', name: 'Morning routine' }),
      routineTemplate({
        id: 'b',
        name: 'Old plan',
        isActive: false,
        recurrence: {
          frequency: 'selectedWeekdays',
          interval: 1,
          startDate: '2026-07-30',
          endDate: null,
          selectedWeekdays: ['monday', 'wednesday'],
        },
      }),
    ]);

    expect(query<HTMLElement>('#active-routines-title').parentElement?.parentElement?.textContent)
      .toContain('Morning routine');
    expect(pageText()).toContain('Every week');
    expect(pageText()).toContain('On Monday, Wednesday');
    expect(pageText()).toContain('Inactive');
  });

  it('creates a routine with the start date the server decided', () => {
    load([]);

    clickByText('New routine');
    setValue('#routine-name', 'Monday - Chest');
    submit();
    flushAntiforgery();

    const request = http.expectOne('/api/routines');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      name: 'Monday - Chest',
      isActive: true,
      recurrence: { frequency: 'weekly', interval: 1, startDate: '2026-07-30' },
    });

    request.flush(routineTemplate());
    fixture.detectChanges();
  });

  it('shows weekday checkboxes only for a selected-weekdays rule and requires one', () => {
    load([]);

    clickByText('New routine');

    expect(query<HTMLElement>('.weekdays', true)).toBeNull();

    select('#routine-frequency', 'selectedWeekdays');
    setValue('#routine-name', 'Gym');
    submit();

    http.expectNone('/api/routines');
    expect(query<HTMLElement>('[role="alert"]').textContent).toContain('at least one weekday');
  });

  it('rejects a whitespace-only routine name without calling the API', () => {
    load([]);

    clickByText('New routine');
    setValue('#routine-name', '   ');
    submit();

    http.expectNone('/api/routines');
    expect(query<HTMLElement>('[role="alert"]').textContent).toContain('Enter a name');
  });

  it('renders a routine name containing markup as text', () => {
    load([routineTemplate({ name: '<script>alert(1)</script>' })]);

    const title = query<HTMLElement>('.record__title');

    expect(title.querySelector('script')).toBeNull();
    expect(title.textContent).toContain('<script>alert(1)</script>');
  });

  it('writes nothing to browser storage', () => {
    load([routineTemplate()]);

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  function load(routines: readonly RoutineTemplate[]): void {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    for (const request of http.match((candidate) => candidate.url === '/api/routines')) {
      request.flush(routines);
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

  function submit(): void {
    query<HTMLFormElement>('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function setValue(selector: string, value: string): void {
    const input = query<HTMLInputElement>(selector);
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function select(selector: string, value: string): void {
    const element = query<HTMLSelectElement>(selector);
    element.value = value;
    element.dispatchEvent(new Event('change'));
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
