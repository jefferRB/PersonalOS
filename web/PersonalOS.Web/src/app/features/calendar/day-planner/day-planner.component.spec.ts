import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import {
  TEST_ITEM_ID,
  calendarDay,
  calendarMonth,
  calendarOccurrence,
  planningItem,
  upcomingWeek,
  userProfile,
} from '../../../../testing/api-fixtures';
import { CalendarDay } from '../../../core/calendar/calendar.models';
import { httpErrorInterceptor } from '../../../core/http/http-error.interceptor';
import { CalendarComponent } from '../calendar.component';

/**
 * The planner is exercised through the calendar page rather than in isolation, because it depends
 * on the page's store and because "clicking a day opens it" is the behaviour worth protecting.
 */
describe('DayPlannerComponent', () => {
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
    vi.restoreAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('opens as a modal dialog with an accessible name', () => {
    load();
    openPlanner();

    const dialog = query<HTMLDialogElement>('dialog');

    expect(dialog.open).toBe(true);
    expect(dialog.getAttribute('aria-labelledby')).toBe('planner-title');
    expect(query<HTMLElement>('#planner-title').textContent).toContain('Thursday, July 30');
  });

  it('opens with no editor, so the timeline gets the whole dialog', () => {
    load();
    openPlanner();

    // Greeting the user with a blank form they did not ask for is exactly what this avoids.
    expect(query<HTMLElement>('.editor', true)).toBeNull();
    expect(query<HTMLElement>('.planner__body').classList).not.toContain(
      'planner__body--editing',
    );
  });

  it('opens the editor from New activity', () => {
    load();
    openPlanner();

    plannerButton('New activity')?.click();
    fixture.detectChanges();

    expect(query<HTMLElement>('#activity-editor-title').textContent).toContain('New activity');
    expect(value('#activity-start-date')).toBe('2026-07-30');
  });

  it('opens the editor from a timeline slot with the date and time filled in', () => {
    load();
    openPlanner();

    // The default window starts at 06:00, so the thirteenth quarter-hour slot is 09:00.
    slots()[12].click();
    fixture.detectChanges();

    expect(value('#activity-start-date')).toBe('2026-07-30');
    expect(value('#activity-start-time')).toBe('09:00');
  });

  it('closes the editor without closing the planner', () => {
    load();
    openPlanner();
    slots()[0].click();
    fixture.detectChanges();

    expect(query<HTMLElement>('.editor', true)).not.toBeNull();

    query<HTMLButtonElement>('.editor .button--ghost').click();
    fixture.detectChanges();

    // Saving or abandoning one activity is not a reason to lose the day being worked through.
    expect(query<HTMLElement>('.editor', true)).toBeNull();
    expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
  });

  it('returns focus to the slot that opened the editor', () => {
    load();
    openPlanner();

    const slot = slots()[12];
    slot.focus();
    slot.click();
    fixture.detectChanges();

    query<HTMLButtonElement>('.editor .button--ghost').click();
    fixture.detectChanges();

    expect(document.activeElement).toBe(slot);
  });

  it('builds the timeline from the configured start, end and interval', () => {
    load({ profile: { dayStartTime: '09:00:00', dayEndTime: '12:00:00', slotMinutes: 60 } });
    openPlanner();

    const labels = slots().map((slot) => slot.getAttribute('aria-label'));

    expect(labels).toEqual([
      'Add an activity at 09:00',
      'Add an activity at 10:00',
      'Add an activity at 11:00',
    ]);
  });

  it('renders slots as semantic buttons', () => {
    load();
    openPlanner();

    // Real buttons, not clickable divs, so the whole day is reachable with a keyboard.
    expect(slots().every((slot) => slot.tagName === 'BUTTON')).toBe(true);
  });

  it('marks the current time only on today', () => {
    load();
    openPlanner();

    // 13:24 is 29 whole quarter-hours after the 06:00 default start.
    expect(query<HTMLElement>('.timeline__now').style.gridRow).toBe('30');
  });

  it('draws no current-time marker on another date', () => {
    load();
    openPlanner();

    query<HTMLButtonElement>('.planner__nav [aria-label="Next day"]').click();
    fixture.detectChanges();
    flushDay(calendarDay({ date: '2026-07-31', todayLocalDate: '2026-07-30' }));
    settle();

    expect(query<HTMLElement>('.timeline__now', true)).toBeNull();
  });

  it('shows anytime activities above the timeline', () => {
    load({
      day: calendarDay({
        occurrences: [
          calendarOccurrence({ planningItemId: 'a', title: 'Call the bank', startTime: null }),
        ],
      }),
    });
    openPlanner();

    expect(query<HTMLElement>('.planner__anytime').textContent).toContain('Call the bank');
  });

  it('places two overlapping activities side by side instead of hiding one', () => {
    load({
      day: calendarDay({
        occurrences: [
          calendarOccurrence({
            planningItemId: 'a',
            title: 'Standup',
            startTime: '09:00:00',
            endTime: '10:00:00',
          }),
          calendarOccurrence({
            planningItemId: 'b',
            title: 'Dentist',
            startTime: '09:30:00',
            endTime: '10:30:00',
          }),
        ],
      }),
    });
    openPlanner();

    const blocks = queryAll('.timeline__blocks > li');

    expect(blocks.length).toBe(2);
    expect(blocks[0].style.gridColumn).toBe('1');
    expect(blocks[1].style.gridColumn).toBe('2');
  });

  it('offers activities outside the visible hours instead of dropping them', () => {
    load({
      day: calendarDay({
        occurrences: [
          calendarOccurrence({ planningItemId: 'a', title: 'Dawn run', startTime: '05:00:00' }),
          calendarOccurrence({ planningItemId: 'b', title: 'Standup', startTime: '09:00:00' }),
        ],
      }),
    });
    openPlanner();

    const outside = query<HTMLElement>('.planner__outside');

    expect(outside.textContent).toContain('Outside visible hours');
    expect(outside.textContent).toContain('Dawn run');
    expect(outside.textContent).not.toContain('Standup');
  });

  it('loads the full item before editing, so a repeating rule is never rewritten blindly', () => {
    load({
      day: calendarDay({
        occurrences: [
          calendarOccurrence({
            planningItemId: TEST_ITEM_ID,
            title: 'Stretch',
            startTime: '07:00:00',
            isRecurring: true,
          }),
        ],
      }),
    });
    openPlanner();

    query<HTMLButtonElement>('.timeline__block').click();
    fixture.detectChanges();

    http.expectOne(`/api/calendar/items/${TEST_ITEM_ID}`).flush(
      planningItem({
        title: 'Stretch',
        recurrence: { frequency: 'weekly', interval: 2, endDate: null, selectedWeekdays: [] },
      }),
    );
    fixture.detectChanges();

    expect(query<HTMLElement>('#activity-editor-title').textContent).toContain('Edit activity');
    expect(value('#activity-title')).toBe('Stretch');
    expect(value('#activity-frequency')).toBe('weekly');
  });

  it('disables the repetition controls once the server has frozen the pattern', () => {
    load({
      day: calendarDay({
        occurrences: [
          calendarOccurrence({ planningItemId: TEST_ITEM_ID, startTime: '07:00:00' }),
        ],
      }),
    });
    openPlanner();

    query<HTMLButtonElement>('.timeline__block').click();
    fixture.detectChanges();

    http.expectOne(`/api/calendar/items/${TEST_ITEM_ID}`).flush(
      planningItem({
        recurrence: { frequency: 'daily', interval: 1, endDate: null, selectedWeekdays: [] },
        isRecurrencePatternLocked: true,
      }),
    );
    fixture.detectChanges();

    expect(query<HTMLSelectElement>('#activity-frequency').disabled).toBe(true);
    expect(query<HTMLElement>('.alert--warning').textContent).toContain(
      'completed or cancelled a day',
    );
  });

  it('refuses an end time that is not after the start time without calling the API', () => {
    load();
    openPlanner();
    slots()[0].click();
    fixture.detectChanges();

    setValue('#activity-title', 'Meeting');
    setValue('#activity-start-time', '10:00');
    setValue('#activity-end-time', '09:00');
    submitEditor();

    http.expectNone('/api/calendar/items');
    expect(query<HTMLElement>('.editor [role="alert"]').textContent).toContain(
      'end time must be after the start time',
    );
  });

  it('creates an activity and closes only the editor', () => {
    load();
    openPlanner();
    slots()[12].click();
    fixture.detectChanges();

    setValue('#activity-title', 'Dentist');
    select('#activity-kind', 'appointment');
    submitEditor();
    flushAntiforgery();

    const request = http.expectOne('/api/calendar/items');

    expect(request.request.body).toMatchObject({
      title: 'Dentist',
      kind: 'appointment',
      startDate: '2026-07-30',
      startTime: '09:00',
    });

    request.flush(planningItem({ title: 'Dentist' }));
    fixture.detectChanges();
    settle();

    expect(query<HTMLElement>('.editor', true)).toBeNull();
    expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
  });

  it('places a server field message on the field it belongs to', () => {
    load();
    openPlanner();
    slots()[0].click();
    fixture.detectChanges();

    setValue('#activity-title', 'Dentist');
    submitEditor();
    flushAntiforgery();

    http.expectOne('/api/calendar/items').flush(
      {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { title: ['Enter a title of 200 characters or fewer.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    expect(query<HTMLElement>('#activity-title-error').textContent).toContain('Enter a title');
  });

  it('resets itself however the platform closed it, including Escape', () => {
    load();
    openPlanner();
    slots()[0].click();
    fixture.detectChanges();

    // Escape and a backdrop click both reach the component as the dialog's own `close` event, which
    // is what this dispatches. The runner's DOM implements neither, which is exactly why the
    // platform element is worth using in a real browser and worth simulating here.
    query<HTMLDialogElement>('dialog').dispatchEvent(new Event('close'));
    fixture.detectChanges();

    expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(false);
    expect(query<HTMLElement>('.editor', true)).toBeNull();
  });

  it('navigates to the next day from inside the planner', () => {
    load();
    openPlanner();

    query<HTMLButtonElement>('.planner__nav [aria-label="Next day"]').click();
    fixture.detectChanges();
    flushDay(calendarDay({ date: '2026-07-31' }));
    settle();

    expect(query<HTMLElement>('#planner-title').textContent).toContain('Friday, July 31');
    // The agenda behind the dialog follows, so closing the planner lands on the same day.
    expect(query<HTMLElement>('#agenda-title').textContent).toContain("Tomorrow's agenda");
  });


  describe('click-outside dismissal', () => {
    it('closes only the editor when the overlay behind it is clicked', () => {
      load();
      openPlanner();
      slots()[0].click();
      fixture.detectChanges();

      query<HTMLButtonElement>('.planner__overlay').click();
      fixture.detectChanges();

      expect(query<HTMLElement>('.editor', true)).toBeNull();
      expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
    });

    it('returns focus to the slot after an outside click closes the editor', () => {
      load();
      openPlanner();

      const slot = slots()[12];
      slot.focus();
      slot.click();
      fixture.detectChanges();

      query<HTMLButtonElement>('.planner__overlay').click();
      fixture.detectChanges();

      expect(document.activeElement).toBe(slot);
    });

    it('does not close the editor when a click lands inside it', () => {
      load();
      openPlanner();
      slots()[0].click();
      fixture.detectChanges();

      query<HTMLInputElement>('#activity-title').click();
      fixture.detectChanges();

      expect(query<HTMLElement>('.editor', true)).not.toBeNull();
    });

    it('asks before discarding a half-written activity', () => {
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

      load();
      openPlanner();
      slots()[0].click();
      fixture.detectChanges();
      setValue('#activity-title', 'Half written');

      query<HTMLButtonElement>('.planner__overlay').click();
      fixture.detectChanges();

      // The same confirmation the application already uses when leaving unsaved work. Refusing it
      // keeps the editor and everything typed into it.
      expect(confirmSpy).toHaveBeenCalled();
      expect(query<HTMLElement>('.editor', true)).not.toBeNull();
      expect(value('#activity-title')).toBe('Half written');
    });

    it('discards a dirty editor once the user confirms', () => {
      vi.spyOn(window, 'confirm').mockReturnValue(true);

      load();
      openPlanner();
      slots()[0].click();
      fixture.detectChanges();
      setValue('#activity-title', 'Half written');

      query<HTMLButtonElement>('.planner__overlay').click();
      fixture.detectChanges();

      expect(query<HTMLElement>('.editor', true)).toBeNull();
      expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
    });

    it('closes the planner when the dialog backdrop is clicked', () => {
      load();
      openPlanner();

      clickBackdrop();

      expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(false);
    });

    it('leaves the planner open when the click lands on the dialog surface', () => {
      load();
      openPlanner();

      clickDialogSurface();

      expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
    });

    it('dismisses the editor first when the backdrop is clicked with it open', () => {
      load();
      openPlanner();
      slots()[0].click();
      fixture.detectChanges();

      clickBackdrop();

      // Losing a half-written activity and the whole day at once, from one stray click, would be
      // the worst possible reading of a backdrop click.
      expect(query<HTMLElement>('.editor', true)).toBeNull();
      expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
    });

    it('ignores a backdrop click when the dialog reports no size', () => {
      load();
      openPlanner();

      const dialog = query<HTMLDialogElement>('dialog');

      // The runner's DOM reports a zero rectangle for every element. A zero rectangle would make
      // every click look like a backdrop click, so it is treated as "not the backdrop" and the
      // explicit controls stay responsible for dismissal.
      expect(dialog.getBoundingClientRect().width).toBe(0);

      dialog.dispatchEvent(
        new MouseEvent('mousedown', { clientX: 5, clientY: 5, bubbles: true }),
      );
      fixture.detectChanges();

      expect(query<HTMLDialogElement>('dialog').hasAttribute('open')).toBe(true);
    });
  });

  /**
   * Clicks where a real browser would report a backdrop click.
   *
   * A native modal has no backdrop node, so the browser reports the click on the dialog itself.
   * `event.target` alone cannot tell a backdrop click from a click on the dialog's own padding, so
   * the component compares coordinates against the dialog's rectangle. The runner's DOM measures
   * nothing, so the rectangle is stubbed to give the coordinates something real to fall outside of.
   */
  function clickBackdrop(): void {
    const dialog = query<HTMLDialogElement>('dialog');

    withBounds(dialog, () =>
      dialog.dispatchEvent(
        new MouseEvent('mousedown', { clientX: 5, clientY: 5, bubbles: true }),
      ),
    );
    fixture.detectChanges();
  }

  /** Clicks a point inside the stubbed dialog rectangle. */
  function clickDialogSurface(): void {
    const dialog = query<HTMLDialogElement>('dialog');

    withBounds(dialog, () =>
      dialog.dispatchEvent(
        new MouseEvent('mousedown', { clientX: 400, clientY: 300, bubbles: true }),
      ),
    );
    fixture.detectChanges();
  }

  function withBounds(element: HTMLElement, run: () => void): void {
    const original = element.getBoundingClientRect;

    element.getBoundingClientRect = () =>
      ({ left: 100, top: 100, right: 700, bottom: 500, width: 600, height: 400 }) as DOMRect;

    try {
      run();
    } finally {
      element.getBoundingClientRect = original;
    }
  }

  function load(
    responses: {
      day?: CalendarDay;
      profile?: { dayStartTime: string; dayEndTime: string; slotMinutes: number };
    } = {},
  ): void {
    flushDay(responses.day ?? calendarDay());

    for (const request of http.match((candidate) => candidate.url === '/api/calendar/upcoming')) {
      if (!request.cancelled) {
        request.flush(upcomingWeek());
      }
    }

    for (const request of http.match((candidate) => candidate.url === '/api/profile')) {
      request.flush(
        responses.profile === undefined
          ? userProfile()
          : userProfile({ calendarDisplay: responses.profile }),
      );
    }

    for (const request of http.match((candidate) => candidate.url === '/api/calendar/month')) {
      if (!request.cancelled) {
        request.flush(calendarMonth());
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


  function openPlanner(): void {
    const button = queryAll('.page__actions button').find((candidate) =>
      candidate.textContent?.includes('New activity'),
    );
    button?.click();
    fixture.detectChanges();
  }

  function plannerButton(label: string): HTMLButtonElement | undefined {
    return queryAll('.planner__header-actions button').find((button) =>
      button.textContent?.includes(label),
    ) as HTMLButtonElement | undefined;
  }

  function slots(): HTMLButtonElement[] {
    return queryAll('.timeline__slot') as HTMLButtonElement[];
  }

  function submitEditor(): void {
    query<HTMLFormElement>('.editor').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
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

  function value(selector: string): string {
    return query<HTMLInputElement | HTMLSelectElement>(selector).value;
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
