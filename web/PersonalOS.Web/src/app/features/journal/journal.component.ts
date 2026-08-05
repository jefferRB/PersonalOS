import { Component, ElementRef, ViewChild, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize, take } from 'rxjs';

import { formLevelMessage, toApiError } from '../../core/errors/problem-details';
import { optionalTrimmedLength, trimToNull } from '../../core/forms/validators';
import { JournalEntry } from '../../core/journal/journal.models';
import { JournalService } from '../../core/journal/journal.service';
import { UnsavedChangesAware } from '../../core/navigation/unsaved-changes.guard';
import { IsoLocalDate, formatDayLabel } from '../../core/time/local-date';
import { TodayService } from '../../core/today/today.service';

/** Maximum length of one reflection section, matching the server. */
const SECTION_MAX_LENGTH = 4000;

/**
 * The daily reflection.
 *
 * This screen holds the most sensitive text in PersonalOS, so its rules are strict and none of
 * them is incidental:
 *
 * - the text lives in component state and in the form only, never in `localStorage`,
 *   `sessionStorage`, or IndexedDB;
 * - it is rendered through interpolation, never through `[innerHTML]`, so a reflection that
 *   contains markup is shown as the characters the user typed;
 * - it travels in a request body, never in a query string, so it cannot reach a browser history
 *   entry or a server access log;
 * - the server answers with `Cache-Control: no-store`;
 * - leaving with unsaved edits asks for confirmation, because a lost reflection cannot be
 *   reconstructed from anywhere else.
 *
 * There is deliberately no sentiment analysis and no interpretation of what was written.
 */
@Component({
  selector: 'app-journal',
  imports: [ReactiveFormsModule],
  templateUrl: './journal.component.html',
  styleUrl: './journal.component.scss',
})
export class JournalComponent implements UnsavedChangesAware {
  private readonly journalService = inject(JournalService);
  private readonly todayService = inject(TodayService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  @ViewChild('statusRegion') private statusRegion?: ElementRef<HTMLElement>;

  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly formError = signal<string | null>(null);
  protected readonly saveSuccess = signal<string | null>(null);
  protected readonly selectedDate = signal<IsoLocalDate>('');
  protected readonly entry = signal<JournalEntry | null>(null);

  protected readonly form = this.formBuilder.group({
    wentWell: this.formBuilder.control('', [optionalTrimmedLength(SECTION_MAX_LENGTH)]),
    wentPoorly: this.formBuilder.control('', [optionalTrimmedLength(SECTION_MAX_LENGTH)]),
    cause: this.formBuilder.control('', [optionalTrimmedLength(SECTION_MAX_LENGTH)]),
    lesson: this.formBuilder.control('', [optionalTrimmedLength(SECTION_MAX_LENGTH)]),
    adjustmentForTomorrow: this.formBuilder.control('', [
      optionalTrimmedLength(SECTION_MAX_LENGTH),
    ]),
    freeNotes: this.formBuilder.control('', [optionalTrimmedLength(SECTION_MAX_LENGTH)]),
  });

  protected readonly dayLabel = computed(() => formatDayLabel(this.selectedDate()));

  protected readonly hasUnsavedEdits = signal(false);

  protected readonly canSave = computed(
    () => !this.isLoading() && !this.isSaving() && this.hasUnsavedEdits(),
  );

  private baseline = '';

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.hasUnsavedEdits.set(JSON.stringify(this.form.getRawValue()) !== this.baseline);
      this.saveSuccess.set(null);
      this.formError.set(null);
    });

    this.todayService
      .getSummary()
      .pipe(take(1))
      .subscribe({
        next: (summary) => {
          this.selectedDate.set(summary.localDate);
          this.load();
        },
        error: (error: unknown) => {
          this.loadError.set(formLevelMessage(toApiError(error)));
          this.isLoading.set(false);
        },
      });
  }

  /** Used by the route guard so a reflection is never discarded silently. */
  hasUnsavedChanges(): boolean {
    return this.hasUnsavedEdits();
  }

  protected load(): void {
    const date = this.selectedDate();

    if (date.length === 0) {
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);

    this.journalService
      .get(date)
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (entry) => this.applyEntry(entry),
        error: (error: unknown) => this.loadError.set(formLevelMessage(toApiError(error))),
      });
  }

  protected onDateChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;

    if (value.length === 0 || value === this.selectedDate()) {
      return;
    }

    if (
      this.hasUnsavedEdits()
      && !window.confirm('You have unsaved changes. Change day and discard them?')
    ) {
      // Put the control back on the day the user is still editing.
      (event.target as HTMLInputElement).value = this.selectedDate();

      return;
    }

    this.selectedDate.set(value);
    this.saveSuccess.set(null);
    this.load();
  }

  protected save(): void {
    if (this.isSaving() || !this.canSave()) {
      return;
    }

    this.formError.set(null);
    this.saveSuccess.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError.set(`Each section must be ${SECTION_MAX_LENGTH} characters or fewer.`);

      return;
    }

    this.isSaving.set(true);
    const value = this.form.getRawValue();

    this.journalService
      .save(this.selectedDate(), {
        wentWell: trimToNull(value.wentWell),
        wentPoorly: trimToNull(value.wentPoorly),
        cause: trimToNull(value.cause),
        lesson: trimToNull(value.lesson),
        adjustmentForTomorrow: trimToNull(value.adjustmentForTomorrow),
        freeNotes: trimToNull(value.freeNotes),
      })
      .pipe(
        take(1),
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: (entry) => {
          this.applyEntry(entry);
          this.saveSuccess.set('Reflection saved.');
          queueMicrotask(() => this.statusRegion?.nativeElement.focus());
        },
        error: (error: unknown) => {
          const apiError = toApiError(error);
          const firstFieldMessage = Object.values(apiError.validationErrors)[0]?.[0];

          this.formError.set(firstFieldMessage ?? formLevelMessage(apiError));
        },
      });
  }

  private applyEntry(entry: JournalEntry): void {
    this.entry.set(entry);

    const value = {
      wentWell: entry.wentWell ?? '',
      wentPoorly: entry.wentPoorly ?? '',
      cause: entry.cause ?? '',
      lesson: entry.lesson ?? '',
      adjustmentForTomorrow: entry.adjustmentForTomorrow ?? '',
      freeNotes: entry.freeNotes ?? '',
    };

    // What was saved becomes the new baseline, so the form reports itself as clean again.
    this.form.setValue(value, { emitEvent: false });
    this.form.markAsPristine();
    this.baseline = JSON.stringify(value);
    this.hasUnsavedEdits.set(false);
  }
}
