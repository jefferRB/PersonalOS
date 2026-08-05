import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { studyProject, studySession, todaySummary } from '../../../testing/api-fixtures';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { StudyProject, StudySession } from '../../core/study/study.models';
import { StudyComponent } from './study.component';

describe('StudyComponent', () => {
  let fixture: ComponentFixture<StudyComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudyComponent],
      providers: [
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(StudyComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('asks for the Monday-to-Sunday week containing the server day', () => {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();
    http.expectOne('/api/study/projects').flush([]);

    const request = http.expectOne((candidate) => candidate.url === '/api/study/sessions');

    // 2026-07-30 is a Thursday, so the week runs from Monday the 27th to Sunday 2 August.
    expect(request.request.params.get('from')).toBe('2026-07-27');
    expect(request.request.params.get('to')).toBe('2026-08-02');

    request.flush([]);
    fixture.detectChanges();
  });

  it('lays the week out Monday to Sunday', () => {
    load();

    const days = queryAll('.week-day h3').map((element) => element.textContent?.trim() ?? '');

    expect(days[0]).toContain('Monday');
    expect(days[6]).toContain('Sunday');
    expect(days.length).toBe(7);
  });

  it('groups sessions into their day and totals the minutes', () => {
    load(
      [studyProject()],
      [
        studySession({ localDate: '2026-07-27', durationMinutes: 45 }),
        studySession({ id: 'b', localDate: '2026-07-27', durationMinutes: 30 }),
        studySession({ id: 'c', localDate: '2026-07-30', durationMinutes: 90 }),
      ],
    );

    const totals = queryAll('.week-day__total').map((element) => element.textContent?.trim());

    expect(totals[0]).toBe('1 h 15 min');
    expect(totals[3]).toBe('1 h 30 min');
    expect(pageText()).toContain('2 h 45 min this week');
  });

  it('creates a study project', () => {
    load();

    clickByText('New project');
    setValue('#project-name', 'Angular');
    submit('form');
    flushAntiforgery();

    const request = http.expectOne('/api/study/projects');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({ name: 'Angular', status: 'active' });

    request.flush(studyProject());
    fixture.detectChanges();
    load([studyProject()]);

    expect(pageText()).toContain('Project created');
  });

  it('records a session against a project', () => {
    load([studyProject()]);

    clickByText('Record session');
    setValue('#session-minutes', '50');
    setValue('#session-summary', 'Signals and forms');
    submit('form');
    flushAntiforgery();

    const request = http.expectOne('/api/study/sessions');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toMatchObject({
      studyProjectId: '5e5b1d1a-4444-4a2b-9c3d-000000000001',
      localDate: '2026-07-30',
      durationMinutes: 50,
      summary: 'Signals and forms',
    });

    request.flush(studySession());
    fixture.detectChanges();
    load([studyProject()], [studySession()]);

    expect(pageText()).toContain('Study session recorded');
  });

  it('refuses a resource link that is not http or https', () => {
    load([studyProject()]);

    clickByText('New project');
    setValue('#project-name', 'Angular');
    clickByText('Add resource');
    setValue('#resource-title-0', 'Notes');
    setValue('#resource-url-0', 'javascript:alert(1)');
    submit('form');

    // The client refuses it, and the server would refuse it again.
    http.expectNone('/api/study/projects');
    expect(query<HTMLElement>('.field__error').textContent).toContain('http://');
  });

  it('accepts an https resource link', () => {
    load([studyProject()]);

    clickByText('New project');
    setValue('#project-name', 'Angular');
    clickByText('Add resource');
    setValue('#resource-title-0', 'Angular signals guide');
    setValue('#resource-url-0', 'https://angular.dev/guide/signals');
    submit('form');
    flushAntiforgery();

    const request = http.expectOne('/api/study/projects');

    expect(request.request.body.resources[0]).toMatchObject({
      title: 'Angular signals guide',
      externalUrl: 'https://angular.dev/guide/signals',
    });

    request.flush(studyProject());
    fixture.detectChanges();
    load([studyProject()]);
  });

  it('renders a saved link with safe attributes and never as HTML', () => {
    const project = studyProject({
      resources: [
        {
          id: 'resource-1',
          title: 'Angular signals guide',
          resourceType: 'article',
          externalUrl: 'https://angular.dev/guide/signals',
          notes: null,
        },
      ],
    });

    load([project]);

    clickByText('Show material');

    const link = query<HTMLAnchorElement>('.resource-links a');

    expect(link.getAttribute('href')).toBe('https://angular.dev/guide/signals');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.textContent).toContain('Angular signals guide');
  });

  it('renders a project name containing markup as text', () => {
    load([studyProject({ name: '<em>Angular</em>' })]);

    const title = queryAll('.record__title').find((element) =>
      element.textContent?.includes('Angular'),
    );

    expect(title?.querySelector('em')).toBeNull();
    expect(title?.textContent).toContain('<em>Angular</em>');
  });

  it('writes nothing to browser storage', () => {
    load([studyProject()], [studySession({ summary: 'Private study note' })]);

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  function load(
    projects: readonly StudyProject[] = [],
    sessions: readonly StudySession[] = [],
  ): void {
    for (const request of http.match('/api/today')) {
      request.flush(todaySummary());
    }

    fixture.detectChanges();

    for (const request of http.match('/api/study/projects')) {
      request.flush(projects);
    }

    for (const request of http.match((candidate) => candidate.url === '/api/study/sessions')) {
      request.flush(sessions);
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
