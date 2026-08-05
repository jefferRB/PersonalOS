import { CalendarOccurrence } from '../calendar/calendar.models';
import { NutritionDay } from '../nutrition/nutrition.models';
import { RoutineOccurrence } from '../routines/routines.models';
import { StudySession } from '../study/study.models';
import { IsoLocalDate } from '../time/local-date';

/**
 * Counts that describe how one local day is going.
 *
 * Every number is counted from data the user entered. There is no streak, score, or trend,
 * because none could be derived honestly from a single day of history.
 */
export interface TodayProgress {
  readonly plannedItemCount: number;
  readonly completedItemCount: number;
  readonly routineCount: number;
  readonly completedRoutineCount: number;
  readonly studyMinutes: number;
  readonly consumedCalories: number;
  readonly dailyCalorieTarget: number | null;
  readonly journalCompleted: boolean;
}

/**
 * Everything the Today screen shows for one local calendar day.
 *
 * The summary carries no journal text. Whether the day was reflected on is enough for Today; the
 * reflection itself is fetched only by the journal screen.
 */
export interface TodaySummary {
  readonly localDate: IsoLocalDate;
  readonly timeZoneId: string;
  readonly isToday: boolean;
  /**
   * The account's current local time as `HH:mm:ss`, decided by the server.
   *
   * The timeline uses it to mark "now". Reading the browser clock instead would put the marker in
   * the wrong place for anyone whose device is not in their saved time zone.
   */
  readonly localTimeOfDay: string;
  /**
   * The day's calendar occurrences, in the same shape the calendar endpoints return.
   *
   * Today and Calendar read one projection. A second task model would be a permanent source of
   * disagreement between the screen that plans a day and the screen that works through it.
   */
  readonly occurrences: readonly CalendarOccurrence[];
  readonly routines: readonly RoutineOccurrence[];
  readonly nutrition: NutritionDay;
  readonly studySessions: readonly StudySession[];
  readonly progress: TodayProgress;
}
