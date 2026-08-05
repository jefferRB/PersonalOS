import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  FilterOption,
  KIND_FILTER_OPTIONS,
  KindFilter,
  OccurrenceFilter,
  ViewFilter,
} from '../../../core/calendar/occurrence-filters';

/**
 * The filter row shared by the daily agenda and the seven-day section.
 *
 * One component serves both because the question is the same in each: which kinds, and which
 * states. Only the option lists differ, and the importance toggle is shown where it earns its
 * place. Every control is a labelled form control rather than a row of toggle buttons, so screen
 * readers announce both the name and the current value.
 */
@Component({
  selector: 'app-occurrence-filters',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './occurrence-filters.component.html',
  styleUrl: './occurrence-filters.component.scss',
})
export class OccurrenceFiltersComponent {
  /** What the section is currently filtered to. */
  readonly filter = input.required<OccurrenceFilter>();

  /** View options this section offers. */
  readonly viewOptions = input.required<readonly FilterOption<ViewFilter>[]>();

  /** Whether to offer the importance toggle. */
  readonly showImportantOnly = input(false);

  /** Whether the filter still matches its defaults, which hides the Clear control. */
  readonly isDefault = input(true);

  /** Prefix for the control identifiers, so two filter rows on one page stay distinct. */
  readonly idPrefix = input.required<string>();

  /** Accessible name of the whole group. */
  readonly label = input('Filters');

  /** How many occurrences the current filter is hiding, announced beside the controls. */
  readonly hiddenCount = input(0);

  readonly filterChange = output<OccurrenceFilter>();

  readonly cleared = output<void>();

  protected readonly kindOptions = KIND_FILTER_OPTIONS;

  protected readonly kindId = computed(() => `${this.idPrefix()}-kind`);

  protected readonly viewId = computed(() => `${this.idPrefix()}-view`);

  protected readonly importantId = computed(() => `${this.idPrefix()}-important`);

  protected onKindChange(value: string): void {
    this.filterChange.emit({ ...this.filter(), kind: value as KindFilter });
  }

  protected onViewChange(value: string): void {
    this.filterChange.emit({ ...this.filter(), view: value as ViewFilter });
  }

  protected onImportantChange(checked: boolean): void {
    this.filterChange.emit({ ...this.filter(), importantOnly: checked });
  }
}
