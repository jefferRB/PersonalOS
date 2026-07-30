import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthStore } from '../../core/auth/auth.store';

interface WorkflowStage {
  readonly name: string;
  readonly description: string;
}

@Component({
  selector: 'app-today',
  imports: [RouterLink],
  templateUrl: './today.component.html',
  styleUrl: './today.component.scss',
})
export class TodayComponent {
  private readonly authStore = inject(AuthStore);
  private readonly dateFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  protected readonly currentUser = this.authStore.currentUser;
  protected readonly localDate = this.dateFormatter.format(new Date());
  protected readonly workflow: WorkflowStage[] = [
    {
      name: 'Capture',
      description: 'Collect the thoughts, obligations, and decisions that need a place.',
    },
    {
      name: 'Plan',
      description: 'Choose what deserves attention without pretending every future module exists.',
    },
    {
      name: 'Execute',
      description: 'Create a calm place to act once task persistence is introduced.',
    },
    {
      name: 'Record',
      description: 'Reserve space for truthful activity history without fake metrics.',
    },
    {
      name: 'Review',
      description: 'Prepare for patterns and weekly reflection after real data exists.',
    },
    {
      name: 'Adjust',
      description: 'Keep improvement practical, private, and explainable.',
    },
  ];
}
