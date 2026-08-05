import { IsoLocalDate, Weekday } from '../time/local-date';

/** Repetition pattern, exactly as the API spells it. */
export type RecurrenceFrequency =
  | 'none'
  | 'daily'
  | 'weekly'
  | 'selectedWeekdays'
  | 'monthly';

/** What a routine is mostly about, exactly as the API spells it. */
export type RoutineCategory = 'general' | 'workout' | 'study' | 'meal' | 'wellbeing';

/** What kind of result a step expects, exactly as the API spells it. */
export type RoutineStepType = 'checklist' | 'exercise' | 'timed' | 'note';

/** Options offered by the repetition picker. */
export const RECURRENCE_FREQUENCIES: readonly {
  value: RecurrenceFrequency;
  label: string;
}[] = [
  { value: 'none', label: 'Does not repeat' },
  { value: 'daily', label: 'Every day' },
  { value: 'weekly', label: 'Every week' },
  { value: 'selectedWeekdays', label: 'On chosen weekdays' },
  { value: 'monthly', label: 'Every month' },
];

/** Options offered by the routine category picker. */
export const ROUTINE_CATEGORIES: readonly { value: RoutineCategory; label: string }[] = [
  { value: 'general', label: 'General' },
  { value: 'workout', label: 'Workout' },
  { value: 'study', label: 'Study' },
  { value: 'meal', label: 'Meal' },
  { value: 'wellbeing', label: 'Wellbeing' },
];

/** Options offered by the step type picker. */
export const ROUTINE_STEP_TYPES: readonly { value: RoutineStepType; label: string }[] = [
  { value: 'checklist', label: 'Checklist' },
  { value: 'exercise', label: 'Exercise' },
  { value: 'timed', label: 'Timed' },
  { value: 'note', label: 'Note' },
];

/** A recurrence rule, as returned by the routine endpoints. */
export interface Recurrence {
  readonly frequency: RecurrenceFrequency;
  readonly interval: number;
  readonly startDate: IsoLocalDate;
  readonly endDate: IsoLocalDate | null;
  readonly selectedWeekdays: readonly Weekday[];
}

/** One target step of a routine. */
export interface RoutineStep {
  readonly id: string;
  readonly order: number;
  readonly title: string;
  readonly stepType: RoutineStepType;
  readonly targetSets: number | null;
  readonly targetRepetitions: number | null;
  readonly targetWeight: number | null;
  readonly targetDurationMinutes: number | null;
  readonly notes: string | null;
}

/** A routine with its ordered steps. */
export interface RoutineTemplate {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly category: RoutineCategory;
  readonly recurrence: Recurrence;
  readonly isActive: boolean;
  readonly steps: readonly RoutineStep[];
}

/** What actually happened for one step during one session. */
export interface RoutineStepResult {
  readonly routineStepId: string;
  readonly isCompleted: boolean;
  readonly actualSets: number | null;
  readonly actualRepetitions: number | null;
  readonly actualWeight: number | null;
  readonly actualDurationMinutes: number | null;
  readonly notes: string | null;
}

/** One execution of a routine on one local calendar day. */
export interface RoutineSession {
  readonly id: string;
  readonly routineTemplateId: string;
  readonly routineName: string;
  readonly category: RoutineCategory;
  readonly localDate: IsoLocalDate;
  readonly startedAtUtc: string;
  readonly completedAtUtc: string | null;
  readonly notes: string | null;
  readonly steps: readonly RoutineStep[];
  readonly stepResults: readonly RoutineStepResult[];
}

/**
 * A routine that applies to one local calendar day.
 *
 * An occurrence with no `sessionId` exists only as a calculation on the server. Nothing is stored
 * for that day until the user starts the routine.
 */
export interface RoutineOccurrence {
  readonly routineTemplateId: string;
  readonly name: string;
  readonly category: RoutineCategory;
  readonly localDate: IsoLocalDate;
  readonly stepCount: number;
  readonly sessionId: string | null;
  readonly isCompleted: boolean;
  readonly completedStepCount: number;
}

/** Values sent for a recurrence rule. */
export interface RecurrenceRequest {
  readonly frequency: RecurrenceFrequency;
  readonly interval: number;
  readonly startDate: IsoLocalDate;
  readonly endDate: IsoLocalDate | null;
  readonly selectedWeekdays: readonly Weekday[];
}

/** Values sent for one routine step. */
export interface RoutineStepRequest {
  readonly title: string;
  readonly stepType: RoutineStepType;
  readonly targetSets: number | null;
  readonly targetRepetitions: number | null;
  readonly targetWeight: number | null;
  readonly targetDurationMinutes: number | null;
  readonly notes: string | null;
}

/** Values sent when creating or editing a routine. */
export interface SaveRoutineRequest {
  readonly name: string;
  readonly description: string | null;
  readonly category: RoutineCategory;
  readonly recurrence: RecurrenceRequest;
  readonly isActive: boolean;
  readonly steps: readonly RoutineStepRequest[];
}

/** Values sent for one step result. */
export interface RoutineStepResultRequest {
  readonly routineStepId: string;
  readonly isCompleted: boolean;
  readonly actualSets: number | null;
  readonly actualRepetitions: number | null;
  readonly actualWeight: number | null;
  readonly actualDurationMinutes: number | null;
  readonly notes: string | null;
}

/** Values sent when saving progress on a session. */
export interface SaveRoutineSessionRequest {
  readonly notes: string | null;
  readonly isCompleted: boolean;
  readonly stepResults: readonly RoutineStepResultRequest[];
}

/** Describes a recurrence rule in one readable English sentence fragment. */
export function describeRecurrence(recurrence: Recurrence): string {
  const every = recurrence.interval > 1 ? `every ${recurrence.interval} ` : 'every ';

  switch (recurrence.frequency) {
    case 'none':
      return 'Does not repeat';
    case 'daily':
      return capitalize(`${every}${recurrence.interval > 1 ? 'days' : 'day'}`);
    case 'weekly':
      return capitalize(`${every}${recurrence.interval > 1 ? 'weeks' : 'week'}`);
    case 'monthly':
      return capitalize(`${every}${recurrence.interval > 1 ? 'months' : 'month'}`);
    case 'selectedWeekdays': {
      const days = recurrence.selectedWeekdays.map(capitalize).join(', ');

      return days.length === 0 ? 'On chosen weekdays' : `On ${days}`;
    }
  }
}

function capitalize(value: string): string {
  return value.length === 0 ? value : value[0].toUpperCase() + value.slice(1);
}
