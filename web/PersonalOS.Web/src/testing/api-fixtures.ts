import {
  CalendarDay,
  CalendarDaySummary,
  CalendarMonth,
  CalendarOccurrence,
  PlanningItem,
  UpcomingWeek,
} from '../app/core/calendar/calendar.models';
import { UserProfile } from '../app/core/profile/profile.models';
import { NutritionDay, NutritionGoal } from '../app/core/nutrition/nutrition.models';
import {
  RoutineOccurrence,
  RoutineSession,
  RoutineTemplate,
} from '../app/core/routines/routines.models';
import { StudyProject, StudySession } from '../app/core/study/study.models';
import { TodaySummary } from '../app/core/today/today.models';

/**
 * Builders for the API shapes the daily screens consume.
 *
 * The fixtures live outside `src/app` and are excluded from the application build, so nothing
 * here can be imported by production code by accident. Every builder takes an override object so
 * a test states only the field it cares about, which keeps each test readable and stops an
 * unrelated field change from rewriting every spec.
 */

/** Fixed local date used by the suite, so no test depends on when it runs. */
export const TEST_LOCAL_DATE = '2026-07-30';

export const TEST_ITEM_ID = '5e5b1d1a-1111-4a2b-9c3d-000000000001';

export function planningItem(overrides: Partial<PlanningItem> = {}): PlanningItem {
  return {
    id: TEST_ITEM_ID,
    title: 'Train',
    description: null,
    kind: 'task',
    category: 'fitness',
    priority: 'normal',
    startDate: TEST_LOCAL_DATE,
    startTime: '07:00:00',
    endTime: null,
    recurrence: {
      frequency: 'none',
      interval: 1,
      endDate: null,
      selectedWeekdays: [],
    },
    isRecurrencePatternLocked: false,
    ...overrides,
  };
}

export function calendarOccurrence(
  overrides: Partial<CalendarOccurrence> = {},
): CalendarOccurrence {
  return {
    planningItemId: TEST_ITEM_ID,
    occurrenceDate: TEST_LOCAL_DATE,
    title: 'Train',
    description: null,
    kind: 'task',
    category: 'fitness',
    priority: 'normal',
    startTime: '07:00:00',
    endTime: null,
    status: 'planned',
    isRecurring: false,
    isImportant: false,
    completedAtUtc: null,
    ...overrides,
  };
}

export function calendarDaySummary(
  overrides: Partial<CalendarDaySummary> = {},
): CalendarDaySummary {
  return {
    date: TEST_LOCAL_DATE,
    totalCount: 1,
    completedCount: 0,
    failedCount: 0,
    cancelledCount: 0,
    kinds: [{ kind: 'task', count: 1 }],
    hasHighPriority: false,
    ...overrides,
  };
}

export function calendarMonth(overrides: Partial<CalendarMonth> = {}): CalendarMonth {
  return {
    year: 2026,
    month: 7,
    fromDate: '2026-06-29',
    toDate: '2026-08-09',
    todayLocalDate: TEST_LOCAL_DATE,
    timeZoneId: 'America/Costa_Rica',
    days: [],
    ...overrides,
  };
}

export function calendarDay(overrides: Partial<CalendarDay> = {}): CalendarDay {
  return {
    date: TEST_LOCAL_DATE,
    todayLocalDate: TEST_LOCAL_DATE,
    timeZoneId: 'America/Costa_Rica',
    localTimeOfDay: '13:24:00',
    occurrences: [],
    ...overrides,
  };
}

export function upcomingWeek(overrides: Partial<UpcomingWeek> = {}): UpcomingWeek {
  return {
    fromDate: TEST_LOCAL_DATE,
    toDate: '2026-08-05',
    todayLocalDate: TEST_LOCAL_DATE,
    timeZoneId: 'America/Costa_Rica',
    days: [],
    ...overrides,
  };
}

export function userProfile(overrides: Partial<UserProfile> = {}): UserProfile {
  return {
    displayName: 'Jefferson',
    email: 'user@example.com',
    timeZoneId: 'America/Costa_Rica',
    calendarDisplay: {
      dayStartTime: '06:00:00',
      dayEndTime: '22:00:00',
      slotMinutes: 15,
    },
    updatedAtUtc: '2026-07-30T13:00:00+00:00',
    ...overrides,
  };
}

export function nutritionGoal(overrides: Partial<NutritionGoal> = {}): NutritionGoal {
  return {
    dailyCalorieTarget: null,
    proteinTargetGrams: null,
    carbohydrateTargetGrams: null,
    fatTargetGrams: null,
    updatedAtUtc: null,
    ...overrides,
  };
}

export function nutritionDay(overrides: Partial<NutritionDay> = {}): NutritionDay {
  return {
    localDate: TEST_LOCAL_DATE,
    goal: nutritionGoal(),
    consumedCalories: 0,
    remainingCalories: null,
    proteinGrams: 0,
    carbohydrateGrams: 0,
    fatGrams: 0,
    meals: [],
    ...overrides,
  };
}

export function routineOccurrence(
  overrides: Partial<RoutineOccurrence> = {},
): RoutineOccurrence {
  return {
    routineTemplateId: '5e5b1d1a-2222-4a2b-9c3d-000000000001',
    name: 'Monday - Chest',
    category: 'workout',
    localDate: TEST_LOCAL_DATE,
    stepCount: 3,
    sessionId: null,
    isCompleted: false,
    completedStepCount: 0,
    ...overrides,
  };
}

export function routineTemplate(overrides: Partial<RoutineTemplate> = {}): RoutineTemplate {
  return {
    id: '5e5b1d1a-2222-4a2b-9c3d-000000000001',
    name: 'Monday - Chest',
    description: null,
    category: 'workout',
    isActive: true,
    recurrence: {
      frequency: 'weekly',
      interval: 1,
      startDate: TEST_LOCAL_DATE,
      endDate: null,
      selectedWeekdays: [],
    },
    steps: [],
    ...overrides,
  };
}

export function routineSession(overrides: Partial<RoutineSession> = {}): RoutineSession {
  return {
    id: '5e5b1d1a-3333-4a2b-9c3d-000000000001',
    routineTemplateId: '5e5b1d1a-2222-4a2b-9c3d-000000000001',
    routineName: 'Monday - Chest',
    category: 'workout',
    localDate: TEST_LOCAL_DATE,
    startedAtUtc: '2026-07-30T13:00:00+00:00',
    completedAtUtc: null,
    notes: null,
    steps: [],
    stepResults: [],
    ...overrides,
  };
}

export function studyProject(overrides: Partial<StudyProject> = {}): StudyProject {
  return {
    id: '5e5b1d1a-4444-4a2b-9c3d-000000000001',
    name: 'Angular',
    description: null,
    status: 'active',
    resources: [],
    ...overrides,
  };
}

export function studySession(overrides: Partial<StudySession> = {}): StudySession {
  return {
    id: '5e5b1d1a-5555-4a2b-9c3d-000000000001',
    studyProjectId: '5e5b1d1a-4444-4a2b-9c3d-000000000001',
    projectName: 'Angular',
    localDate: TEST_LOCAL_DATE,
    startTime: null,
    durationMinutes: 45,
    summary: null,
    progressNote: null,
    ...overrides,
  };
}

export function todaySummary(overrides: Partial<TodaySummary> = {}): TodaySummary {
  return {
    localDate: TEST_LOCAL_DATE,
    timeZoneId: 'America/Costa_Rica',
    isToday: true,
    localTimeOfDay: '13:24:00',
    occurrences: [],
    routines: [],
    nutrition: nutritionDay(),
    studySessions: [],
    progress: {
      plannedItemCount: 0,
      completedItemCount: 0,
      routineCount: 0,
      completedRoutineCount: 0,
      studyMinutes: 0,
      consumedCalories: 0,
      dailyCalorieTarget: null,
      journalCompleted: false,
    },
    ...overrides,
  };
}
