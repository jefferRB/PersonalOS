import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import {
  CalendarDay,
  CalendarOccurrence,
  SavePlanningItemRequest,
} from '../../../core/calendar/calendar.models';
import { layOutDay } from '../../../core/calendar/timeline-layout';
import { UNSAVED_CHANGES_MESSAGE } from '../../../core/navigation/unsaved-changes.guard';
import { IsoLocalDate, formatDayLabel, toMinutesOfDay } from '../../../core/time/local-date';
import {
  ActivityCardComponent,
  OccurrenceStatusChange,
} from '../activity-card/activity-card.component';
import { ActivityEditorComponent } from '../activity-editor/activity-editor.component';
import { CalendarStore } from '../calendar.store';
import { DayTimelineComponent } from '../day-timeline/day-timeline.component';

/**
 * The day planner.
 *
 * It is a native `<dialog>` opened with `showModal()`. That gives the focus trap, the Escape
 * handling, the backdrop, and the inertness of everything behind it from the platform, all of which
 * a hand-rolled overlay would have to reimplement and would get subtly wrong.
 *
 * The timeline owns the dialog. The editor is a drawer that appears only once the user asks for it,
 * by pressing New activity, choosing a slot, or opening an existing activity, and closing it leaves
 * the planner where it was. Opening a day no longer greets the user with a blank form they did not
 * ask for.
 *
 * There is one vertical scroll container, so the wheel always moves the thing the user expects.
 * Focus is captured from whatever opened the dialog and from whatever opened the editor, and handed
 * back to each when the matching thing closes.
 */
@Component({
  selector: 'app-day-planner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ActivityCardComponent, ActivityEditorComponent, DayTimelineComponent],
  templateUrl: './day-planner.component.html',
  styleUrl: './day-planner.component.scss',
})
export class DayPlannerComponent {
  protected readonly store = inject(CalendarStore);

  readonly isOpen = input.required<boolean>();

  readonly date = input.required<IsoLocalDate>();

  readonly day = input<CalendarDay | null>(null);

  readonly isLoading = input(false);

  /** The account's current local day, so the Today control knows where to go. */
  readonly todayDate = input<IsoLocalDate | null>(null);

  readonly closed = output<void>();

  readonly dayOffset = output<number>();

  readonly goToToday = output<void>();

  readonly statusChange = output<OccurrenceStatusChange>();

  private readonly dialogRef = viewChild<ElementRef<HTMLDialogElement>>('dialog');
  private readonly scrollRef = viewChild<ElementRef<HTMLElement>>('scroll');
  private readonly editorRef = viewChild(ActivityEditorComponent);

  /** The element that had focus when the dialog opened, so it can be given focus back. */
  private opener: HTMLElement | null = null;

  /** The element that opened the editor, so focus returns there rather than to the top. */
  private editorOpener: HTMLElement | null = null;

  /** The date the timeline was last scrolled for, so a re-render does not fight the user. */
  private scrolledFor: IsoLocalDate | null = null;

  protected readonly occurrences = computed(() => this.day()?.occurrences ?? []);

  protected readonly dayLabel = computed(() => formatDayLabel(this.date()));

  protected readonly isEditorOpen = computed(() => this.store.editor().mode !== 'closed');

  protected readonly anytime = computed(() =>
    this.occurrences().filter((occurrence) => occurrence.startTime === null),
  );

  /** Timed activities the configured window does not reach, offered rather than dropped. */
  protected readonly outsideWindow = computed(
    () => layOutDay(this.occurrences(), this.store.timelineWindow()).outsideWindow,
  );

  protected readonly isEmpty = computed(
    () => !this.isLoading() && this.occurrences().length === 0,
  );

  /** "Now" is only meaningful on the account's actual current day. */
  protected readonly localTimeOfDay = computed(() => {
    const current = this.day();

    return current !== null && current.date === current.todayLocalDate
      ? current.localTimeOfDay
      : null;
  });

  /** Whether the planner is already on the account's current day. */
  protected readonly isToday = computed(
    () => this.todayDate() !== null && this.date() === this.todayDate(),
  );

  protected readonly isSaving = this.store.isSaving;
  protected readonly isDeleting = this.store.isDeleting;
  protected readonly editor = this.store.editor;

  constructor() {
    effect(() => {
      const dialog = this.dialogRef()?.nativeElement;

      if (dialog === undefined) {
        return;
      }

      if (this.isOpen() && !dialog.open) {
        this.opener = document.activeElement as HTMLElement | null;
        this.showModal(dialog);
        this.scrolledFor = null;
      } else if (!this.isOpen() && dialog.open) {
        this.closeDialog(dialog);
      }
    });

    // Scrolling happens once per day shown. Doing it on every render would yank the view back
    // whenever anything else changed while the user was reading.
    effect(() => {
      const date = this.date();
      const occurrences = this.occurrences();

      if (!this.isOpen() || this.scrolledFor === date) {
        return;
      }

      const container = this.scrollRef()?.nativeElement;

      if (container === undefined) {
        return;
      }

      this.scrolledFor = date;
      queueMicrotask(() => this.scrollToStartingPoint(container, occurrences));
    });
  }

  /** Handles the dialog closing for any reason, including Escape and the backdrop. */
  protected onDialogClose(): void {
    this.closed.emit();
    this.opener?.focus();
    this.opener = null;
    this.editorOpener = null;
  }

  protected requestClose(): void {
    const dialog = this.dialogRef()?.nativeElement;

    if (dialog !== undefined) {
      this.closeDialog(dialog);
    }
  }

  /**
   * Decides what a click on the dialog element itself should dismiss.
   *
   * A native modal reports clicks on its backdrop as clicks on the dialog, because the backdrop is
   * a pseudo-element with no node of its own. `event.target` alone cannot separate the two: browsers
   * disagree about what it points at, and a click on padding inside the dialog names the dialog too.
   * Comparing the pointer's coordinates against the dialog's own rectangle is the reliable test.
   *
   * The fallback matters for the test runner, whose DOM reports a zero-sized rectangle for every
   * element. A zero rectangle would make every click look like a backdrop click, so an empty
   * rectangle is treated as "not the backdrop" and dismissal falls to the explicit controls.
   */
  protected onDialogPointerDown(event: MouseEvent): void {
    const dialog = this.dialogRef()?.nativeElement;

    if (dialog === undefined || event.target !== dialog) {
      return;
    }

    const bounds = dialog.getBoundingClientRect();

    if (bounds.width === 0 || bounds.height === 0) {
      return;
    }

    const isOutside =
      event.clientX < bounds.left
      || event.clientX > bounds.right
      || event.clientY < bounds.top
      || event.clientY > bounds.bottom;

    if (!isOutside) {
      return;
    }

    // With the editor open the backdrop deals with the editor first. Losing a half-written activity
    // and the whole day at once, from one stray click, would be the worst possible reading of it.
    if (this.isEditorOpen()) {
      this.tryCloseEditor();

      return;
    }

    this.requestClose();
  }

  /** Dismisses the editor when a click lands on the timeline behind it. */
  protected onOverlayClick(): void {
    this.tryCloseEditor();
  }

  /**
   * Closes the editor unless doing so would discard something the user typed.
   *
   * A pristine form closes straight away. A dirty one goes through the same confirmation the
   * application already uses when navigating away from unsaved work, so the two behave alike.
   */
  protected tryCloseEditor(): void {
    if (this.editorRef()?.hasUnsavedChanges() === true && !window.confirm(UNSAVED_CHANGES_MESSAGE)) {
      return;
    }

    this.closeEditor();
  }

  /** Opens the editor for a brand new activity, optionally at the time of a clicked slot. */
  protected openCreate(time: string | null): void {
    this.rememberEditorOpener();
    this.store.openCreateEditor(time);
  }

  /** Opens the editor for an existing activity. */
  protected openOccurrence(occurrence: CalendarOccurrence): void {
    this.rememberEditorOpener();
    this.store.openEditEditor(occurrence.planningItemId);
  }

  /**
   * Closes the editor and hands focus back to whatever opened it.
   *
   * The planner stays exactly where it was. Saving one activity is not a reason to lose the day the
   * user was working through.
   */
  protected closeEditor(): void {
    this.store.closeEditor();

    const opener = this.editorOpener;
    this.editorOpener = null;

    // The element is only refocused when it is still on screen; a slot can disappear when the
    // configured window changes underneath it.
    if (opener !== null && opener.isConnected) {
      opener.focus();
    }
  }

  protected onSave(request: SavePlanningItemRequest): void {
    this.store.save(request, () => this.closeEditor());
  }

  protected onDelete(itemId: string): void {
    const startDate = this.store.editor().item?.startDate ?? this.date();

    this.store.delete(itemId, startDate, () => this.closeEditor());
  }

  private rememberEditorOpener(): void {
    this.editorOpener = document.activeElement as HTMLElement | null;
  }

  /**
   * Puts the timeline where the user is most likely to need it.
   *
   * On today that is the current time, because a planner that opens at six in the morning is
   * useless at four in the afternoon. On any other date it is the first activity, or the top of the
   * configured window when the day is empty.
   */
  private scrollToStartingPoint(
    container: HTMLElement,
    occurrences: readonly CalendarOccurrence[],
  ): void {
    const window = this.store.timelineWindow();
    const now = toMinutesOfDay(this.localTimeOfDay());
    const firstTimed = occurrences
      .map((occurrence) => toMinutesOfDay(occurrence.startTime))
      .filter((minutes): minutes is number => minutes !== null)
      .sort((left, right) => left - right)[0];

    const target = now ?? firstTimed ?? window.startMinutes;
    const timeline = container.querySelector<HTMLElement>('.timeline__grid');

    // The guard covers environments whose DOM has no scrolling, the test runner's among them.
    // Positioning the view is a nicety; failing to do it must never break the planner.
    if (
      timeline === null
      || window.intervalMinutes <= 0
      || typeof container.scrollTo !== 'function'
    ) {
      return;
    }

    const rows = Math.max(
      1,
      Math.ceil((window.endMinutes - window.startMinutes) / window.intervalMinutes),
    );
    const rowHeight = timeline.offsetHeight / rows;
    const offsetRows = (target - window.startMinutes) / window.intervalMinutes;
    // A little headroom keeps the target off the very top edge, where it reads as cut off.
    const top = timeline.offsetTop + offsetRows * rowHeight - rowHeight * 2;

    container.scrollTo({ top: Math.max(0, top), behavior: 'auto' });
  }

  /**
   * Opens the dialog modally.
   *
   * The guard is for environments whose `dialog` implementation is incomplete, the test runner's
   * DOM among them: falling back to the `open` attribute keeps the content reachable instead of
   * leaving an invisible dialog behind.
   */
  private showModal(dialog: HTMLDialogElement): void {
    if (typeof dialog.showModal === 'function') {
      dialog.showModal();
    } else {
      dialog.setAttribute('open', '');
    }
  }

  /**
   * Closes the dialog.
   *
   * The fallback emits the same `close` event the platform would, so the component has one path
   * back out whichever implementation it is running on.
   */
  private closeDialog(dialog: HTMLDialogElement): void {
    if (typeof dialog.close === 'function') {
      dialog.close();

      return;
    }

    dialog.removeAttribute('open');
    dialog.dispatchEvent(new Event('close'));
  }
}
