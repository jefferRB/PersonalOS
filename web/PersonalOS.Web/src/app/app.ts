import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';

import { AuthStore } from './core/auth/auth.store';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  imports: [RouterOutlet],
  styleUrl: './app.scss'
})
export class App {
  private readonly authStore = inject(AuthStore);

  protected readonly isStartupLoading = this.authStore.isStartupLoading;

  constructor() {
    this.authStore.initialize().pipe(takeUntilDestroyed()).subscribe();
  }
}
