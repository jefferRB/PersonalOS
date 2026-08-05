import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { todaySummary } from '../../../testing/api-fixtures';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { JournalEntry } from '../../core/journal/journal.models';
import { JournalComponent } from './journal.component';

describe('JournalComponent', () => {
  let fixture: ComponentFixture<JournalComponent>;
  let http: HttpTestingController;

  const emptyEntry: JournalEntry = {
    localDate: '2026-07-30',
    wentWell: null,
    wentPoorly: null,
    cause: null,
    lesson: null,
    adjustmentForTomorrow: null,
    freeNotes: null,
    updatedAtUtc: null,
    hasContent: false,
  };

  const writtenEntry: JournalEntry = {
    ...emptyEntry,
    wentWell: 'Finished the migration review.',
    lesson: 'Reading the generated SQL first saves an hour.',
    updatedAtUtc: '2026-07-30T22:00:00+00:00',
    hasContent: true,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [JournalComponent],
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(JournalComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('reads the entry through the path, never through a query string', () => {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    const request = http.expectOne('/api/journal/2026-07-30');

    expect(request.request.method).toBe('GET');
    expect(request.request.urlWithParams).toBe('/api/journal/2026-07-30');

    request.flush(emptyEntry);
    fixture.detectChanges();
  });

  it('shows an empty day as empty rather than as an error', () => {
    load(emptyEntry);

    expect(query<HTMLElement>('.card__header .chip').textContent?.trim()).toBe('Empty');
    expect(value('#went-well')).toBe('');
  });

  it('loads an existing entry into the six sections', () => {
    load(writtenEntry);

    expect(value('#went-well')).toBe('Finished the migration review.');
    expect(value('#lesson')).toBe('Reading the generated SQL first saves an hour.');
    expect(query<HTMLElement>('.card__header .chip').textContent?.trim()).toBe('Written');
  });

  it('keeps Save disabled until something changes', () => {
    load(emptyEntry);

    expect(query<HTMLButtonElement>('button[type="submit"]').disabled).toBe(true);

    setValue('#went-well', 'A good day.');

    expect(query<HTMLButtonElement>('button[type="submit"]').disabled).toBe(false);
  });

  it('saves the reflection in the request body, with the day in the path', () => {
    load(emptyEntry);

    setValue('#went-well', 'Shipped the milestone.');
    setValue('#lesson', 'Small vertical slices are easier to verify.');
    submit();
    flushAntiforgery();

    const request = http.expectOne('/api/journal/2026-07-30');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toMatchObject({
      wentWell: 'Shipped the milestone.',
      lesson: 'Small vertical slices are easier to verify.',
      wentPoorly: null,
    });

    // The reflection never travels in the URL, so it cannot land in a history entry or an access log.
    expect(request.request.urlWithParams).not.toContain('Shipped');

    request.flush({ ...writtenEntry, wentWell: 'Shipped the milestone.' });
    fixture.detectChanges();

    expect(query<HTMLElement>('[role="status"]').textContent).toContain('Reflection saved');
  });

  it('updates the existing entry instead of creating a second one for the same day', () => {
    load(writtenEntry);

    setValue('#went-well', 'Revised.');
    submit();
    flushAntiforgery();

    const request = http.expectOne('/api/journal/2026-07-30');

    // One entry per day is a server invariant; the client simply PUTs the same address again.
    expect(request.request.method).toBe('PUT');

    request.flush({ ...writtenEntry, wentWell: 'Revised.' });
    fixture.detectChanges();

    expect(query<HTMLElement>('[role="status"]').textContent).toContain('Reflection saved');
  });

  it('marks the form clean again after a successful save', () => {
    load(emptyEntry);

    setValue('#went-well', 'Done.');
    submit();
    flushAntiforgery();
    http.expectOne('/api/journal/2026-07-30').flush({ ...writtenEntry, wentWell: 'Done.' });
    fixture.detectChanges();

    expect(query<HTMLButtonElement>('button[type="submit"]').disabled).toBe(true);
    expect(query<HTMLElement>('.form-actions .chip', true)).toBeNull();
  });

  it('reports unsaved changes so navigation can be guarded', () => {
    load(emptyEntry);

    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);

    setValue('#cause', 'Started too late.');

    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    expect(query<HTMLElement>('.form-actions .chip').textContent).toContain('Unsaved changes');
  });

  it('asks before changing day with unsaved edits, and stays when refused', () => {
    load(emptyEntry);

    setValue('#went-well', 'Half written.');
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    const input = query<HTMLInputElement>('#journal-date');
    input.value = '2026-07-29';
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    // Nothing was requested, and the field went back to the day being edited.
    http.expectNone('/api/journal/2026-07-29');
    expect(input.value).toBe('2026-07-30');
  });

  it('renders a reflection containing markup as text, never as HTML', () => {
    load({ ...writtenEntry, wentWell: '<img src=x onerror="alert(1)">' });

    const textarea = query<HTMLTextAreaElement>('#went-well');

    expect(textarea.value).toBe('<img src=x onerror="alert(1)">');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });

  it('writes no journal text to browser storage', () => {
    load(writtenEntry);

    setValue('#free-notes', 'Something I would never want cached.');

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
    expect(JSON.stringify({ ...localStorage })).not.toContain('never want cached');
    expect(JSON.stringify({ ...sessionStorage })).not.toContain('never want cached');
    expect(JSON.stringify({ ...localStorage })).not.toContain('migration review');
  });

  function load(entry: JournalEntry): void {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    for (const request of http.match(
      (candidate) => candidate.url === `/api/journal/${entry.localDate}`,
    )) {
      request.flush(entry);
    }

    fixture.detectChanges();
  }

  function flushAntiforgery(): void {
    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'test-token' });
    fixture.detectChanges();
  }

  function submit(): void {
    query<HTMLFormElement>('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function setValue(selector: string, newValue: string): void {
    const input = query<HTMLTextAreaElement>(selector);
    input.value = newValue;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function value(selector: string): string {
    return query<HTMLTextAreaElement>(selector).value;
  }

  function query<T extends HTMLElement>(selector: string, allowMissing = false): T {
    const element = fixture.nativeElement.querySelector(selector) as T | null;

    if (!allowMissing) {
      expect(element).not.toBeNull();
    }

    return element as T;
  }
});
