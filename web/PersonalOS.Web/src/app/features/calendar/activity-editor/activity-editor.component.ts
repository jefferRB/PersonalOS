import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';

import {
  PLANNING_CATEGORIES,
  PLANNING_KINDS,
  PLANNING_PRIORITIES,
  PlanningCategory,
  PlanningItem,
  PlanningItemKind,
  PlanningPriority,
  RECURRENCE_FREQUENCIES,
  RecurrenceFrequency,
  SavePlanningItemRequest,
} from '../../../core/calendar/calendar.models';
import { firstValidationError } from '../../../core/errors/problem-details';
import {
  optionalInteger,
  timeRange,
  trimToNull,
  trimValue,
  trimmedLength,
} from '../../../core/forms/validators';
import {
  IsoLocalDate,
  WEEKDAY_VALUES,
  Weekday,
  toInputTime,
} from '../../../core/time/local-date';

/**
 * The panel that creates and edits one activity.
 *
 * The form is the only place times and repetition are changed. That is a deliberate trade for
 * dropping drag, drop, and resize: typing "09:15" always works, on any pointer, with any motor
 * precision, and needs no library.
 *
 * When the server has frozen an item's repetition, the recurrence controls are disabled rather than
 * hidden. Hiding them would leave the user wondering where the setting went; disabling them with an
 * explanation says what happened and why.
 */
@Component({
  selector: 'app-activity-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './activity-editor.component.html',
  styleUrl: './activity-editor.component.scss',
})
export class ActivityEditorComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);

  /** The item being edited, or `null` when creating. */
  readonly item = input<PlanningItem | null>(null);

  /** Day the editor should default to when creating. */
  readonly defaultDate = input.required<IsoLocalDate>();

  /** Time the editor should default to, from the slot the user clicked. */
  readonly defaultTime = input<string | null>(null);

  readonly isSaving = input(false);

  readonly isDeleting = input(false);

  /** Field messages the server returned, keyed by contract field name. */
  readonly serverErrors = input<Record<string, string[]>>({});

  /** A message that belongs to the form as a whole. */
  readonly formError = input<string | null>(null);

  readonly save = output<SavePlanningItemRequest>();

  readonly remove = output<string>();

  readonly cancel = output<void>();

  protected readonly kinds = PLANNING_KINDS;
  protected readonly categories = PLANNING_CATEGORIES;
  protected readonly priorities = PLANNING_PRIORITIES;
  protected readonly frequencies = RECURRENCE_FREQUENCIES;
  protected readonly weekdays = WEEKDAY_VALUES;

  protected readonly selectedWeekdays = signal<readonly Weekday[]>([]);

  /** Mirrors the frequency control so the template can react without reading the form twice. */
  protected readonly frequency = signal<RecurrenceFrequency>('none');

  protected readonly form = this.formBuilder.group(
    {
      title: this.formBuilder.control('', [trimmedLength(1, 200)]),
      description: this.formBuilder.control(''),
      kind: this.formBuilder.control<PlanningItemKind>('task'),
      category: this.formBuilder.control<PlanningCategory>('general'),
      priority: this.formBuilder.control<PlanningPriority>('normal'),
      startDate: this.formBuilder.control(''),
      startTime: this.formBuilder.control(''),
      endTime: this.formBuilder.control(''),
      frequency: this.formBuilder.control<RecurrenceFrequency>('none'),
      interval: this.formBuilder.control('1', [optionalInteger(1, 365)]),
      endDate: this.formBuilder.control(''),
    },
    { validators: [timeRange('startTime', 'endTime')] },
  );

  protected readonly isEditing = computed(() => this.item() !== null);

  protected readonly isRepeating = computed(() => this.frequency() !== 'none');

  protected readonly isWeekly = computed(() => this.frequency() === 'weekly');

  protected readonly isPatternLocked = computed(
    () => this.item()?.isRecurrencePatternLocked ?? false,
  );

  constructor() {
    this.form.controls.frequency.valueChanges.subscribe((value) => this.frequency.set(value));

    // Resetting from the inputs rather than from a lifecycle hook keeps the form in step whichever
    // way the planner changes what is being edited: a new slot, a different activity, or back to
    // creating.
    effect(() => this.resetFrom(this.item(), this.defaultDate(), this.defaultTime()));
  }

  /**
   * Whether closing now would throw away something the user typed.
   *
   * The planner asks before dismissing the editor on an outside click, so a stray click on the
   * timeline never silently discards a half-written activity.
   */
  hasUnsavedChanges(): boolean {
    return this.form.dirty;
  }

  protected fieldError(field: string): string | null {
    return firstValidationError(this.serverErrors(), field);
  }

  protected isWeekdaySelected(weekday: Weekday): boolean {
    return this.selectedWeekdays().includes(weekday);
  }

  protected toggleWeekday(weekday: Weekday): void {
    this.selectedWeekdays.update((current) =>
      current.includes(weekday)
        ? current.filter((value) => value !== weekday)
        : [...current, weekday],
    );
  }

  protected onSubmit(): void {
    if (this.isSaving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    const value = this.form.getRawValue();

    this.save.emit({
      title: trimValue(value.title),
      description: trimToNull(value.description),
      kind: value.kind,
      category: value.category,
      priority: value.priority,
      startDate: value.startDate,
      startTime: trimToNull(value.startTime),
      endTime: trimToNull(value.endTime),
      recurrence: {
        frequency: value.frequency,
        interval: Number(value.interval) || 1,
        endDate: trimToNull(value.endDate),
        selectedWeekdays: value.frequency === 'weekly' ? this.selectedWeekdays() : [],
      },
    });
  }

  protected onDelete(): void {
    const current = this.item();

    if (current !== null) {
      this.remove.emit(current.id);
    }
  }

  /** Describes the client-side problems, so the form never fails silently. */
  protected localError(): string | null {
    if (!this.form.touched) {
      return null;
    }

    if (this.form.hasError('endBeforeStart')) {
      return 'The end time must be after the start time.';
    }

    if (this.form.hasError('endWithoutStart')) {
      return 'Enter a start time before entering an end time.';
    }

    if (this.form.controls.title.invalid) {
      return 'Enter a title of 200 characters or fewer.';
    }

    return null;
  }

  private resetFrom(
    item: PlanningItem | null,
    defaultDate: IsoLocalDate,
    defaultTime: string | null,
  ): void {
    if (item === null) {
      this.form.reset({
        title: '',
        description: '',
        kind: 'task',
        category: 'general',
        priority: 'normal',
        startDate: defaultDate,
        startTime: defaultTime ?? '',
        endTime: '',
        frequency: 'none',
        interval: '1',
        endDate: '',
      });
      this.selectedWeekdays.set([]);
      this.frequency.set('none');

      return;
    }

    this.form.reset({
      title: item.title,
      description: item.description ?? '',
      kind: item.kind,
      category: item.category,
      priority: item.priority,
      startDate: item.startDate,
      startTime: toInputTime(item.startTime),
      endTime: toInputTime(item.endTime),
      frequency: item.recurrence.frequency,
      interval: String(item.recurrence.interval),
      endDate: item.recurrence.endDate ?? '',
    });
    this.selectedWeekdays.set([...item.recurrence.selectedWeekdays]);
    this.frequency.set(item.recurrence.frequency);

    if (item.isRecurrencePatternLocked) {
      this.form.controls.frequency.disable({ emitEvent: false });
      this.form.controls.interval.disable({ emitEvent: false });
      this.form.controls.startDate.disable({ emitEvent: false });
    } else {
      this.form.controls.frequency.enable({ emitEvent: false });
      this.form.controls.interval.enable({ emitEvent: false });
      this.form.controls.startDate.enable({ emitEvent: false });
    }
  }
}
