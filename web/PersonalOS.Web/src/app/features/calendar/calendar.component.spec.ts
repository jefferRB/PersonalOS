import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import {
  TEST_ITEM_ID,
  calendarDay,
  calendarDaySummary,
  calendarMonth,
  calendarOccurrence,
  upcomingWeek,
  userProfile,
} from '../../../testing/api-fixtures';
import { CalendarDay, CalendarMonth, UpcomingWeek } from '../../core/calendar/calendar.models';
import { httpErrorInterceptor } from '../../core/http/http-error.interceptor';
import { CalendarComponent } from './calendar.component';

describe('CalendarComponent', () => {
  let fixture: ComponentFixture<CalendarComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CalendarComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
    sessionStorage.clear();

    fixture = TestBed.createComponent(CalendarComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('asks the server which day it is instead of reading the browser clock', () => {
    // The first day request carries no date at all, which is what lets the server answer from the
    // account's saved time zone.
    const request = http.expectOne((candidate) => candidate.url === '/api/calendar/day');

    expect(request.request.params.has('date')).toBe(false);

    request.flush(calendarDay());
    fixture.detectChanges();
    load();

    expect(query<HTMLElement>('#agenda-title').textContent).toContain("Today's agenda");
    expect(query<HTMLElement>('#month-title').textContent).toContain('July 2026');
  });

  describe('month grid', () => {
    it('renders only the days of the visible month', () => {
      load();

      // July 2026 has 31 days. Nothing from June or August is drawn as a date.
      const numbers = queryAll('.day:not(.day--placeholder) .day__number').map((cell) =>
        cell.textContent?.trim(),
      );

      expect(numbers.length).toBe(31);
      expect(numbers[0]).toBe('1');
      expect(numbers[30]).toBe('31');
    });

    it('pads the first and last weeks with inert placeholders', () => {
      load();

      // 1 July 2026 is a Wednesday, so a Monday-first grid needs two leading blanks, and 35 cells
      // fill five whole weeks.
      const cells = queryAll('.month__grid > *');
      const placeholders = queryAll('.day--placeholder');

      expect(cells.length).toBe(35);
      expect(placeholders.length).toBe(4);
      // A blank is not a date: nothing to focus, nothing to click, nothing announced.
      expect(placeholders.every((cell) => cell.tagName !== 'BUTTON')).toBe(true);
      expect(placeholders.every((cell) => cell.getAttribute('aria-hidden') === 'true')).toBe(true);
      expect(placeholders.every((cell) => !cell.hasAttribute('tabindex'))).toBe(true);
    });

    it('keeps exactly one tab stop in the grid', () => {
      load();

      expect(queryAll('.day[tabindex="0"]').length).toBe(1);
    });

    it('steps over the placeholders rather than landing on one', () => {
      load();

      // The selected day is Thursday 30 July; one step back lands on the 29th, still a real day.
      pressKey('ArrowLeft');
      flushDay(calendarDay({ date: '2026-07-29' }));
      settle();

      expect(agendaHeaderText()).toContain('July 29');
    });

    it('stops at the first day of the month rather than paging away', () => {
      load();

      pressKey('Home');
      flushDay(calendarDay({ date: '2026-07-01' }));
      settle();

      expect(agendaHeaderText()).toContain('July 1');
    });

    it('stops at the last day of the month', () => {
      load();

      pressKey('End');
      flushDay(calendarDay({ date: '2026-07-31' }));
      settle();

      expect(agendaHeaderText()).toContain('July 31');
    });

    it('keeps the month controls inside the calendar card', () => {
      load();

      const card = query<HTMLElement>('.month');

      // The controls only ever act on this grid and the planner it opens, so they live with it.
      expect(card.querySelector('#toolbar-month')).not.toBeNull();
      expect(card.querySelector('#toolbar-year')).not.toBeNull();
      expect(card.querySelector('[aria-label="Previous month"]')).not.toBeNull();
      expect(card.querySelector('[aria-label="Next month"]')).not.toBeNull();
      expect(card.querySelector('#toolbar-start')).not.toBeNull();
      expect(card.querySelector('#toolbar-interval')).not.toBeNull();
    });
  });

  describe('month navigation', () => {
    it('moves to the next month without disturbing the agenda', () => {
      load();

      query<HTMLButtonElement>('[aria-label="Next month"]').click();
      fixture.detectChanges();

      const request = expectMonthRequest();

      expect(request.request.params.get('month')).toBe('8');
      expect(request.request.params.get('year')).toBe('2026');

      request.flush(calendarMonth({ month: 8 }));
      fixture.detectChanges();

      expect(query<HTMLElement>('#month-title').textContent).toContain('August 2026');
      http.expectNone((candidate) => candidate.url === '/api/calendar/day');
    });

    it('moves to the previous month', () => {
      load();

      query<HTMLButtonElement>('[aria-label="Previous month"]').click();
      fixture.detectChanges();

      const request = expectMonthRequest();

      expect(request.request.params.get('month')).toBe('6');

      request.flush(calendarMonth({ month: 6 }));
      fixture.detectChanges();

      expect(query<HTMLElement>('#month-title').textContent).toContain('June 2026');
    });

    it('jumps straight to a month chosen from the picker', () => {
      load();

      select('#toolbar-month', '11');

      const request = expectMonthRequest();

      expect(request.request.params.get('month')).toBe('11');
      expect(request.request.params.get('year')).toBe('2026');

      request.flush(calendarMonth({ month: 11 }));
      fixture.detectChanges();

      expect(query<HTMLElement>('#month-title').textContent).toContain('November 2026');
    });

    it('jumps straight to a year chosen from the picker', () => {
      load();

      select('#toolbar-year', '2028');

      const request = expectMonthRequest();

      expect(request.request.params.get('year')).toBe('2028');
      expect(request.request.params.get('month')).toBe('7');

      request.flush(calendarMonth({ year: 2028 }));
      fixture.detectChanges();

      expect(query<HTMLElement>('#month-title').textContent).toContain('July 2028');
    });

    it('discards a slow month response when the user has already moved on', () => {
      load();

      query<HTMLButtonElement>('[aria-label="Next month"]').click();
      fixture.detectChanges();
      const august = expectMonthRequest();

      select('#toolbar-year', '2029');
      const later = expectMonthRequest();

      // Both paths go through the same subject, so a picker choice cancels a pending arrow request
      // exactly as another arrow press would.
      expect(august.cancelled).toBe(true);

      later.flush(calendarMonth({ year: 2029 }));
      fixture.detectChanges();

      expect(query<HTMLElement>('#month-title').textContent).toContain('2029');
    });
  });

  describe('timeline settings', () => {
    it('loads the account’s saved window into the toolbar', () => {
      load({ profile: { dayStartTime: '08:00:00', dayEndTime: '18:00:00', slotMinutes: 30 } });

      expect(value('#toolbar-start')).toBe('08:00');
      expect(value('#toolbar-end')).toBe('18:00');
      expect(value('#toolbar-interval')).toBe('30');
    });

    it('saves a valid window through the profile endpoint', () => {
      load();

      setValue('#toolbar-start', '07:00');
      setValue('#toolbar-end', '19:00');
      select('#toolbar-interval', '60');
      applySettings();
      flushAntiforgery();

      const request = http.expectOne('/api/profile/calendar-display');

      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({
        dayStartTime: '07:00',
        dayEndTime: '19:00',
        slotMinutes: 60,
      });

      request.flush(
        userProfile({
          calendarDisplay: { dayStartTime: '07:00:00', dayEndTime: '19:00:00', slotMinutes: 60 },
        }),
      );
      fixture.detectChanges();

      expect(query<HTMLElement>('[role="status"]').textContent).toContain(
        'Timeline settings saved',
      );
    });

    it('refuses a start that is not earlier than the end without calling the API', () => {
      load();

      setValue('#toolbar-start', '22:00');
      setValue('#toolbar-end', '06:00');
      applySettings();

      // Reported rather than corrected: silently swapping the values would leave the user with a
      // timeline they did not ask for and no explanation.
      http.expectNone('/api/profile/calendar-display');
      expect(query<HTMLElement>('.month__alert').textContent).toContain(
        'start time must be earlier than the end time',
      );
    });

    it('shows the server’s refusal when it rejects the values', () => {
      load();

      setValue('#toolbar-start', '06:00');
      setValue('#toolbar-end', '22:00');
      applySettings();
      flushAntiforgery();

      http.expectOne('/api/profile/calendar-display').flush(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { slotMinutes: ['Choose an interval of 15, 30, 60 minutes.'] },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      fixture.detectChanges();

      expect(query<HTMLElement>('.month__alert').textContent).toContain('Choose an interval');
    });
  });

  describe('month indicators', () => {
    it('states the totals, the kinds, and the importance in the accessible name', () => {
      load({
        month: calendarMonth({
          days: [
            calendarDaySummary({
              date: '2026-07-30',
              totalCount: 3,
              completedCount: 1,
              kinds: [
                { kind: 'appointment', count: 2 },
                { kind: 'task', count: 1 },
              ],
              hasHighPriority: true,
            }),
          ],
        }),
      });

      const labels = queryAll('.day').map((day) => day.getAttribute('aria-label'));

      expect(labels).toContain(
        'Thursday, July 30, 3 activities, 2 appointments, 1 task, 1 completed, '
          + 'includes something important',
      );
      expect(labels).toContain('Wednesday, July 29, nothing scheduled');
    });

    it('shows a visible important marker as a glyph rather than a colour', () => {
      load({
        month: calendarMonth({
          days: [calendarDaySummary({ date: '2026-07-30', hasHighPriority: true })],
        }),
      });

      expect(query<HTMLElement>('.day--selected .day__important').textContent?.trim()).toBe('!');
    });

    it('shows a kind badge with its count', () => {
      load({
        month: calendarMonth({
          days: [
            calendarDaySummary({
              date: '2026-07-30',
              totalCount: 2,
              kinds: [{ kind: 'event', count: 2 }],
            }),
          ],
        }),
      });

      const badge = query<HTMLElement>('.day--selected .day__kind');

      expect(badge.getAttribute('data-kind')).toBe('event');
      expect(badge.textContent?.trim()).toBe('2');
    });

    it('marks the selected day and today with accessible state, not only colour', () => {
      load();

      const selected = query<HTMLButtonElement>('.day--selected');

      expect(selected.getAttribute('aria-selected')).toBe('true');
      expect(selected.getAttribute('tabindex')).toBe('0');
      expect(query<HTMLButtonElement>('.day--today').getAttribute('aria-current')).toBe('date');
    });
  });

  describe('daily agenda', () => {
    it('names the day the way a person would', () => {
      load();

      expect(query<HTMLElement>('#agenda-title').textContent).toContain("Today's agenda");

      shiftDay('Next day', '2026-07-31');

      expect(query<HTMLElement>('#agenda-title').textContent).toContain("Tomorrow's agenda");
    });

    it('spells out a date that has no name of its own', () => {
      load();

      shiftDay('Next day', '2026-08-04');

      expect(query<HTMLElement>('#agenda-title').textContent).toContain('Agenda for August 4');
    });

    it('moves to the previous day', () => {
      load();

      shiftDay('Previous day', '2026-07-29');

      expect(query<HTMLElement>('#agenda-title').textContent).toContain("Yesterday's agenda");
    });

    it('offers Today only when the agenda has moved away from it', () => {
      load();

      expect(agendaButton('Today')).toBeUndefined();

      shiftDay('Next day', '2026-07-31');

      expect(agendaButton('Today')).toBeDefined();

      agendaButton('Today')?.click();
      fixture.detectChanges();
      settle();

      expect(query<HTMLElement>('#agenda-title').textContent).toContain("Today's agenda");
    });

    it('filters by kind without asking the server again', () => {
      load({
        day: calendarDay({
          occurrences: [
            calendarOccurrence({ planningItemId: 'a', title: 'Dentist', kind: 'appointment' }),
            calendarOccurrence({ planningItemId: 'b', title: 'Email', kind: 'task' }),
          ],
        }),
      });

      select('#agenda-filter-kind', 'appointment');

      // A day is bounded, so a filter is arithmetic rather than a round trip.
      http.expectNone((candidate) => candidate.url === '/api/calendar/day');
      expect(agendaTitles()).toEqual(['Dentist']);
    });

    it('shows completed activities only when the view asks for them', () => {
      load({
        day: calendarDay({
          occurrences: [
            calendarOccurrence({ planningItemId: 'a', title: 'Done', status: 'completed' }),
            calendarOccurrence({ planningItemId: 'b', title: 'Open' }),
          ],
        }),
      });

      // Open is the default, so a finished activity is out of the way until asked for.
      expect(agendaTitles()).toEqual(['Open']);

      select('#agenda-filter-view', 'completed');

      // The whole agenda's text also holds the View option labels, so the cards are what is checked.
      expect(agendaTitles()).toEqual(['Done']);
    });

    it('reports how many activities the filter is hiding', () => {
      load({
        day: calendarDay({
          occurrences: [
            calendarOccurrence({ planningItemId: 'a', status: 'completed' }),
            calendarOccurrence({ planningItemId: 'b' }),
          ],
        }),
      });

      expect(query<HTMLElement>('.filters__hidden').textContent).toContain('1 hidden by filters');
    });

    it('clears the filters back to their defaults', () => {
      load({
        day: calendarDay({ occurrences: [calendarOccurrence({ status: 'completed' })] }),
      });

      select('#agenda-filter-view', 'all');
      clearFilters('agenda-filter-view');

      expect(value('#agenda-filter-view')).toBe('open');
    });

    it('separates anytime activities from timed ones', () => {
      load({
        day: calendarDay({
          occurrences: [
            calendarOccurrence({ planningItemId: 'a', title: 'Call the bank', startTime: null }),
            calendarOccurrence({ planningItemId: 'b', title: 'Dentist', startTime: '09:00:00' }),
          ],
        }),
      });

      const headings = queryAll('.agenda__group').map((heading) => heading.textContent?.trim());

      expect(headings).toEqual(['Anytime', 'Scheduled']);
    });

    it('offers no Complete control on a cancelled activity', () => {
      load({
        day: calendarDay({
          occurrences: [calendarOccurrence({ title: 'Called off', status: 'cancelled' })],
        }),
      });

      select('#agenda-filter-view', 'cancelled');

      const labels = queryAll('.activity__actions button').map((button) =>
        button.textContent?.trim(),
      );

      // An action that cannot apply is absent rather than present and disabled.
      expect(labels.some((label) => label?.startsWith('Complete'))).toBe(false);
      expect(labels.some((label) => label?.startsWith('Restore'))).toBe(true);
    });

    it('records a completion through the occurrence status endpoint', () => {
      load({
        day: calendarDay({
          occurrences: [calendarOccurrence({ planningItemId: TEST_ITEM_ID })],
        }),
      });

      const complete = queryAll('.activity__actions button').find((button) =>
        button.textContent?.includes('Complete'),
      );
      complete?.click();
      fixture.detectChanges();
      flushAntiforgery();

      const request = http.expectOne(
        `/api/calendar/items/${TEST_ITEM_ID}/occurrences/2026-07-30/status`,
      );

      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({ status: 'completed' });

      request.flush(calendarOccurrence({ status: 'completed' }));
      fixture.detectChanges();
      settle();
    });
  });

  describe('next 7 days', () => {
    it('is headed "Next 7 days" and shows only important activities by default', () => {
      load({
        upcoming: upcomingWeek({
          days: [
            {
              date: '2026-08-01',
              occurrences: [
                calendarOccurrence({
                  planningItemId: 'a',
                  title: 'Concert',
                  kind: 'event',
                  isImportant: true,
                }),
                calendarOccurrence({ planningItemId: 'b', title: 'Ordinary email' }),
              ],
            },
          ],
        }),
      });

      expect(query<HTMLElement>('#upcoming-title').textContent).toContain('Next 7 days');
      expect(value('#upcoming-filter-important')).toBe('on');
      expect(upcomingText()).toContain('Concert');
      expect(upcomingText()).not.toContain('Ordinary email');
    });

    it('reveals the rest of the week when Important only is turned off', () => {
      load({
        upcoming: upcomingWeek({
          days: [
            {
              date: '2026-08-01',
              occurrences: [calendarOccurrence({ title: 'Ordinary email' })],
            },
          ],
        }),
      });

      expect(upcomingText()).not.toContain('Ordinary email');

      uncheck('#upcoming-filter-important');

      // The whole window was already loaded, so revealing it costs no request.
      http.expectNone((candidate) => candidate.url === '/api/calendar/upcoming');
      expect(upcomingText()).toContain('Ordinary email');
    });

    it('hides cancelled activities by default', () => {
      load({
        upcoming: upcomingWeek({
          days: [
            {
              date: '2026-08-01',
              occurrences: [
                calendarOccurrence({
                  title: 'Called off',
                  kind: 'event',
                  isImportant: true,
                  status: 'cancelled',
                }),
              ],
            },
          ],
        }),
      });

      expect(upcomingText()).not.toContain('Called off');
    });

    it('groups by date and labels the anytime group', () => {
      load({
        upcoming: upcomingWeek({
          days: [
            {
              date: '2026-08-01',
              occurrences: [
                calendarOccurrence({
                  planningItemId: 'a',
                  title: 'All day thing',
                  kind: 'event',
                  isImportant: true,
                  startTime: null,
                }),
                calendarOccurrence({
                  planningItemId: 'b',
                  title: 'Timed thing',
                  kind: 'event',
                  isImportant: true,
                  startTime: '09:00:00',
                }),
              ],
            },
          ],
        }),
      });

      const groups = queryAll('.upcoming__group').map((group) => group.textContent?.trim());

      expect(groups).toEqual(['Anytime', 'Scheduled']);
    });
  });


  describe('failed outcome', () => {
    it('summarises failed days separately from cancelled ones', () => {
      load({
        month: calendarMonth({
          days: [
            calendarDaySummary({
              date: '2026-07-30',
              totalCount: 2,
              failedCount: 1,
              cancelledCount: 1,
            }),
          ],
        }),
      });

      const labels = queryAll('.day:not(.day--placeholder)').map((day) =>
        day.getAttribute('aria-label'),
      );

      expect(labels).toContain(
        'Thursday, July 30, 2 activities, 1 task, 1 failed, 1 cancelled',
      );
      expect(query<HTMLElement>('.day--selected .day__failed', true)).not.toBeNull();
    });

    it('offers Mark failed on a planned activity that has already arrived', () => {
      load({
        day: calendarDay({ occurrences: [calendarOccurrence({ title: 'Run' })] }),
      });

      expect(agendaActionLabels()).toContain('Mark failed Run');
    });

    it('does not offer Mark failed on a future activity', () => {
      load();

      shiftDay('Next day', '2026-07-31');
      flushDayInto('2026-07-31', [
        calendarOccurrence({ occurrenceDate: '2026-07-31', title: 'Run' }),
      ]);

      // A day that has not arrived has not had its chance yet.
      expect(agendaActionLabels().some((label) => label?.startsWith('Mark failed'))).toBe(false);
    });

    it('records a failed outcome through the shared status endpoint', () => {
      load({
        day: calendarDay({
          occurrences: [calendarOccurrence({ planningItemId: TEST_ITEM_ID, title: 'Run' })],
        }),
      });

      agendaButtonByText('Mark failed')?.click();
      fixture.detectChanges();
      flushAntiforgery();

      const request = http.expectOne(
        `/api/calendar/items/${TEST_ITEM_ID}/occurrences/2026-07-30/status`,
      );

      expect(request.request.method).toBe('PUT');
      expect(request.request.body).toEqual({ status: 'failed' });

      request.flush(calendarOccurrence({ status: 'failed' }));
      fixture.detectChanges();
      settle();
    });

    it('offers Reopen on a failed activity and states the outcome in words', () => {
      load({
        day: calendarDay({
          occurrences: [calendarOccurrence({ title: 'Run', status: 'failed' })],
        }),
      });

      select('#agenda-filter-view', 'failed');

      expect(query<HTMLElement>('.activity__status').textContent).toContain('Failed');
      expect(agendaActionLabels()).toContain('Reopen Run');
      expect(agendaActionLabels().some((label) => label?.startsWith('Complete'))).toBe(false);
    });

    it('excludes failed activities from the open view and shows them under Failed', () => {
      load({
        day: calendarDay({
          occurrences: [
            calendarOccurrence({ planningItemId: 'a', title: 'Missed', status: 'failed' }),
            calendarOccurrence({ planningItemId: 'b', title: 'Still open' }),
          ],
        }),
      });

      expect(agendaTitles()).toEqual(['Still open']);

      select('#agenda-filter-view', 'failed');

      expect(agendaTitles()).toEqual(['Missed']);
    });

    it('keeps failed activities out of the next seven days by default', () => {
      load({
        upcoming: upcomingWeek({
          days: [
            {
              date: '2026-08-01',
              occurrences: [
                calendarOccurrence({
                  title: 'Missed appointment',
                  kind: 'appointment',
                  isImportant: true,
                  status: 'failed',
                }),
              ],
            },
          ],
        }),
      });

      // The section answers "what is coming". Something already missed is not coming.
      expect(upcomingText()).not.toContain('Missed appointment');
    });
  });

  it('opens the day planner when a day is picked and keeps that day when it closes', () => {
    load();

    expect(query<HTMLDialogElement>('dialog').open).toBe(false);

    selectDay('2026-07-31');

    expect(query<HTMLDialogElement>('dialog').open).toBe(true);
    expect(query<HTMLElement>('#planner-title').textContent).toContain('Friday, July 31');

    query<HTMLButtonElement>('[aria-label="Close the day planner"]').click();
    fixture.detectChanges();

    expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(false);
    expect(query<HTMLElement>('#agenda-title').textContent).toContain("Tomorrow's agenda");
  });

  it('returns focus to the day that opened the planner', () => {
    load();

    const dayButton = queryAll('.day').find(
      (day) => day.getAttribute('aria-label')?.startsWith('Friday, July 31') === true,
    ) as HTMLButtonElement;

    dayButton.focus();
    dayButton.click();
    fixture.detectChanges();
    flushDay(calendarDay({ date: '2026-07-31' }));
    fixture.detectChanges();

    query<HTMLButtonElement>('[aria-label="Close the day planner"]').click();
    fixture.detectChanges();

    expect(document.activeElement).toBe(dayButton);
  });

  it('keeps a failed month from blanking out an agenda that loaded', () => {
    flushDay(calendarDay({ occurrences: [calendarOccurrence({ title: 'Dentist' })] }));
    flushUpcoming();
    flushProfile();
    failAllMonthRequests();

    expect(query<HTMLElement>('.month .alert--error').textContent).toContain(
      'PersonalOS could not complete the request',
    );
    expect(agendaText()).toContain('Dentist');
  });

  it('writes nothing to browser storage', () => {
    load({
      day: calendarDay({
        occurrences: [calendarOccurrence({ title: 'Private appointment' })],
      }),
    });

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  /** Answers the four independent requests the page makes on start-up. */
  function load(
    responses: {
      month?: CalendarMonth;
      day?: CalendarDay;
      upcoming?: UpcomingWeek;
      profile?: { dayStartTime: string; dayEndTime: string; slotMinutes: number };
    } = {},
  ): void {
    flushDay(responses.day ?? calendarDay());
    flushUpcoming(responses.upcoming);
    flushProfile(responses.profile);

    for (const request of http.match((candidate) => candidate.url === '/api/calendar/month')) {
      if (!request.cancelled) {
        request.flush(responses.month ?? calendarMonth());
      }
    }

    fixture.detectChanges();
  }

  function flushDay(response: CalendarDay): void {
    for (const request of http.match((candidate) => candidate.url === '/api/calendar/day')) {
      if (!request.cancelled) {
        request.flush(response);
      }
    }

    fixture.detectChanges();
  }

  function flushUpcoming(response?: UpcomingWeek): void {
    for (const request of http.match((candidate) => candidate.url === '/api/calendar/upcoming')) {
      if (!request.cancelled) {
        request.flush(response ?? upcomingWeek());
      }
    }
  }

  function flushProfile(display?: {
    dayStartTime: string;
    dayEndTime: string;
    slotMinutes: number;
  }): void {
    for (const request of http.match((candidate) => candidate.url === '/api/profile')) {
      request.flush(display === undefined ? userProfile() : userProfile({ calendarDisplay: display }));
    }

    fixture.detectChanges();
  }

  function shiftDay(label: string, expectedDate: string): void {
    query<HTMLButtonElement>(`.agenda [aria-label="${label}"]`).click();
    fixture.detectChanges();
    flushDay(calendarDay({ date: expectedDate }));
    settle();
  }

  function selectDay(date: string): void {
    const button = queryAll('.day').find(
      (day) => day.getAttribute('aria-label')?.includes(labelFor(date)) === true,
    );
    button?.click();
    fixture.detectChanges();
    flushDay(calendarDay({ date }));
    fixture.detectChanges();
  }

  function labelFor(date: string): string {
    return new Intl.DateTimeFormat('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      timeZone: 'UTC',
    }).format(new Date(`${date}T00:00:00Z`));
  }

  function agendaButton(label: string): HTMLButtonElement | undefined {
    return queryAll('.agenda__actions button').find(
      (button) => button.textContent?.trim() === label,
    ) as HTMLButtonElement | undefined;
  }

  function agendaText(): string {
    return query<HTMLElement>('.agenda').textContent ?? '';
  }

  /** The agenda's heading and its date subtitle together. */
  function agendaHeaderText(): string {
    return query<HTMLElement>('.agenda__heading').textContent ?? '';
  }

  function pressKey(key: string): void {
    query<HTMLElement>('.month__grid').dispatchEvent(new KeyboardEvent('keydown', { key }));
    fixture.detectChanges();
  }

  /** Accessible labels of the action buttons on the agenda's cards. */
  function agendaActionLabels(): (string | undefined)[] {
    return [...query<HTMLElement>('.agenda').querySelectorAll('.activity__actions button')].map(
      (button) => button.textContent?.replace(/\s+/g, ' ').trim(),
    );
  }

  function agendaButtonByText(text: string): HTMLButtonElement | undefined {
    return [
      ...query<HTMLElement>('.agenda').querySelectorAll('.activity__actions button'),
    ].find((button) => button.textContent?.includes(text)) as HTMLButtonElement | undefined;
  }

  /** Answers a pending day request with a specific date and set of occurrences. */
  function flushDayInto(date: string, occurrences: ReturnType<typeof calendarOccurrence>[]): void {
    for (const request of http.match((candidate) => candidate.url === '/api/calendar/day')) {
      if (!request.cancelled) {
        request.flush(calendarDay({ date, occurrences }));
      }
    }

    fixture.detectChanges();
    settle();
  }

  /** Titles of the cards currently on the agenda, which is what a filter actually changes. */
  function agendaTitles(): string[] {
    return [...query<HTMLElement>('.agenda').querySelectorAll('.activity__title')].map(
      (title) => title.textContent?.trim() ?? '',
    );
  }

  /**
   * Answers every calendar request that is still pending, repeatedly.
   *
   * A refresh can trigger further loads, and `switchMap` cancels some of them along the way, so the
   * helper drains until nothing is left rather than assuming a fixed number.
   */
  function settle(): void {
    for (let pass = 0; pass < 5; pass += 1) {
      const pending = http.match((candidate) => candidate.url.startsWith('/api/calendar'));

      if (pending.length === 0) {
        break;
      }

      for (const request of pending) {
        if (request.cancelled) {
          continue;
        }

        if (request.request.url === '/api/calendar/day') {
          request.flush(calendarDay());
        } else if (request.request.url === '/api/calendar/upcoming') {
          request.flush(upcomingWeek());
        } else {
          request.flush(calendarMonth());
        }
      }

      fixture.detectChanges();
    }
  }


  function upcomingText(): string {
    return (query<HTMLElement>('#upcoming-title').closest('section') as HTMLElement).textContent
      ?? '';
  }

  function applySettings(): void {
    query<HTMLFormElement>('.month__group--settings').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function clearFilters(nearSelector: string): void {
    const group = query<HTMLElement>(`#${nearSelector}`).closest('.filters') as HTMLElement;
    const button = [...group.querySelectorAll('button')].find((candidate) =>
      candidate.textContent?.includes('Clear filters'),
    );
    button?.click();
    fixture.detectChanges();
  }

  /**
   * Fails every pending month request.
   *
   * The day response can settle the anchor and trigger a second load, so the helper drains them all
   * rather than assuming exactly one is in flight.
   */
  function failAllMonthRequests(): void {
    for (const request of http.match((candidate) => candidate.url === '/api/calendar/month')) {
      if (!request.cancelled) {
        request.flush(
          { title: 'Server error.', status: 500 },
          { status: 500, statusText: 'Server Error' },
        );
      }
    }

    fixture.detectChanges();
  }

  function expectMonthRequest() {
    return http.expectOne((candidate) => candidate.url === '/api/calendar/month');
  }

  function flushAntiforgery(): void {
    http.expectOne('/api/antiforgery/token').flush({ requestToken: 'test-token' });
    fixture.detectChanges();
  }

  function setValue(selector: string, next: string): void {
    const input = query<HTMLInputElement>(selector);
    input.value = next;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function select(selector: string, next: string): void {
    const element = query<HTMLSelectElement>(selector);
    element.value = next;
    element.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function uncheck(selector: string): void {
    const element = query<HTMLInputElement>(selector);
    element.checked = false;
    element.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  function value(selector: string): string {
    const element = query<HTMLInputElement | HTMLSelectElement>(selector);

    return element instanceof HTMLInputElement && element.type === 'checkbox'
      ? element.checked
        ? 'on'
        : 'off'
      : element.value;
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
