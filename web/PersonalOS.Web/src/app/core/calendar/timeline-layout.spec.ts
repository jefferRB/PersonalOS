import { calendarOccurrence } from '../../../testing/api-fixtures';
import { CalendarOccurrence } from './calendar.models';
import { TimelineWindow, buildSlots, layOutDay, nowRow } from './timeline-layout';

/** The default window: 06:00 to 22:00 in quarter-hour slots. */
const DEFAULT_WINDOW: TimelineWindow = {
  startMinutes: 6 * 60,
  endMinutes: 22 * 60,
  intervalMinutes: 15,
};

describe('buildSlots', () => {
  it('covers the configured window at the configured resolution', () => {
    const slots = buildSlots(DEFAULT_WINDOW);

    // Sixteen hours at four slots an hour.
    expect(slots.length).toBe(64);
    expect(slots[0].time).toBe('06:00');
    expect(slots[63].time).toBe('21:45');
  });

  it('draws far fewer rows at a coarser interval', () => {
    const slots = buildSlots({ ...DEFAULT_WINDOW, intervalMinutes: 60 });

    expect(slots.length).toBe(16);
    expect(slots.map((slot) => slot.time).slice(0, 3)).toEqual(['06:00', '07:00', '08:00']);
    expect(slots.every((slot) => slot.isHourStart)).toBe(true);
  });

  it('follows a narrower window', () => {
    const slots = buildSlots({ startMinutes: 9 * 60, endMinutes: 11 * 60, intervalMinutes: 30 });

    expect(slots.map((slot) => slot.time)).toEqual(['09:00', '09:30', '10:00', '10:30']);
  });

  it('marks only whole hours as hour starts', () => {
    const slots = buildSlots(DEFAULT_WINDOW);

    expect(slots[0].isHourStart).toBe(true);
    expect(slots[1].isHourStart).toBe(false);
    expect(slots[4].isHourStart).toBe(true);
  });

  it('produces nothing for a window with no hours in it', () => {
    expect(buildSlots({ startMinutes: 600, endMinutes: 600, intervalMinutes: 15 })).toEqual([]);
  });
});

describe('layOutDay', () => {
  it('places an activity relative to the start of the window', () => {
    const layout = layOutDay(
      [occurrence({ startTime: '09:00:00', endTime: '10:00:00' })],
      DEFAULT_WINDOW,
    );

    // 09:00 is twelve quarter-hours after 06:00, and an hour spans four of them.
    expect(layout.blocks[0].rowStart).toBe(13);
    expect(layout.blocks[0].rowSpan).toBe(4);
    expect(layout.columnCount).toBe(1);
  });

  it('follows a coarser interval', () => {
    const layout = layOutDay(
      [occurrence({ startTime: '09:00:00', endTime: '11:00:00' })],
      { ...DEFAULT_WINDOW, intervalMinutes: 60 },
    );

    expect(layout.blocks[0].rowStart).toBe(4);
    expect(layout.blocks[0].rowSpan).toBe(2);
  });

  it('gives a short activity a usable minimum height', () => {
    const layout = layOutDay([occurrence({ startTime: '09:00:00', endTime: null })], DEFAULT_WINDOW);

    // A single quarter-hour block is too small to hold a title or to click reliably, so it is drawn
    // at a half-hour floor even though the activity itself is shorter.
    expect(layout.blocks[0].rowSpan).toBe(2);
  });

  it('leaves untimed activities out, because they belong above the timeline', () => {
    const layout = layOutDay(
      [occurrence({ startTime: null }), occurrence({ startTime: '09:00:00' })],
      DEFAULT_WINDOW,
    );

    expect(layout.blocks.length).toBe(1);
  });

  it('puts overlapping activities in separate columns', () => {
    const layout = layOutDay(
      [
        occurrence({ planningItemId: 'a', startTime: '09:00:00', endTime: '10:00:00' }),
        occurrence({ planningItemId: 'b', startTime: '09:30:00', endTime: '10:30:00' }),
      ],
      DEFAULT_WINDOW,
    );

    expect(layout.columnCount).toBe(2);
    expect(layout.blocks.map((block) => block.column)).toEqual([1, 2]);
  });

  it('reuses a column once its previous activity has finished', () => {
    const layout = layOutDay(
      [
        occurrence({ planningItemId: 'a', startTime: '09:00:00', endTime: '10:00:00' }),
        occurrence({ planningItemId: 'b', startTime: '09:30:00', endTime: '10:30:00' }),
        occurrence({ planningItemId: 'c', startTime: '10:00:00', endTime: '11:00:00' }),
      ],
      DEFAULT_WINDOW,
    );

    // The third starts exactly when the first ends, so it takes that column back rather than
    // opening a third and squeezing the day.
    expect(layout.columnCount).toBe(2);
    expect(layout.blocks.map((block) => block.column)).toEqual([1, 2, 1]);
  });

  it('hands back activities the window does not reach', () => {
    const layout = layOutDay(
      [
        occurrence({ planningItemId: 'early', title: 'Dawn run', startTime: '05:00:00' }),
        occurrence({ planningItemId: 'late', title: 'Night shift', startTime: '23:00:00' }),
        occurrence({ planningItemId: 'inside', title: 'Standup', startTime: '09:00:00' }),
      ],
      DEFAULT_WINDOW,
    );

    // An activity that vanished because of a display setting would be the worst possible outcome of
    // choosing one, so the planner is handed them to show separately.
    expect(layout.blocks.length).toBe(1);
    expect(layout.outsideWindow.map((item) => item.title)).toEqual(['Dawn run', 'Night shift']);
  });

  it('clips an activity that starts before the window but runs into it', () => {
    const layout = layOutDay(
      [occurrence({ startTime: '05:00:00', endTime: '07:00:00' })],
      DEFAULT_WINDOW,
    );

    expect(layout.outsideWindow.length).toBe(0);
    expect(layout.blocks[0].rowStart).toBe(1);
    expect(layout.blocks[0].rowSpan).toBe(4);
  });

  it('never lets a block run past the end of the window', () => {
    const layout = layOutDay(
      [occurrence({ startTime: '21:00:00', endTime: '23:30:00' })],
      DEFAULT_WINDOW,
    );

    expect(layout.blocks[0].rowStart + layout.blocks[0].rowSpan).toBeLessThanOrEqual(65);
  });

  it('reports one column for an empty day, so the grid still renders', () => {
    expect(layOutDay([], DEFAULT_WINDOW).columnCount).toBe(1);
  });
});

describe('nowRow', () => {
  it('places the marker relative to the start of the window', () => {
    // 13:24 is 7 hours 24 minutes after 06:00, which is 29 whole quarter-hours.
    expect(nowRow('13:24:00', DEFAULT_WINDOW)).toBe(30);
  });

  it('follows a coarser interval', () => {
    expect(nowRow('13:24:00', { ...DEFAULT_WINDOW, intervalMinutes: 60 })).toBe(8);
  });

  it('reports nothing when the time is outside the window', () => {
    expect(nowRow('05:00:00', DEFAULT_WINDOW)).toBeNull();
    expect(nowRow('23:00:00', DEFAULT_WINDOW)).toBeNull();
  });

  it('reports nothing on a day that is not today', () => {
    // Drawing a "now" marker on another date would be a lie about what the user is looking at.
    expect(nowRow(null, DEFAULT_WINDOW)).toBeNull();
  });
});

function occurrence(overrides: Partial<CalendarOccurrence>): CalendarOccurrence {
  return calendarOccurrence(overrides);
}
