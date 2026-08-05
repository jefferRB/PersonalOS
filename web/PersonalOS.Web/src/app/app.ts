import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';

import { ThemeService } from './core/appearance/theme.service';
import { AuthStore } from './core/auth/auth.store';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  imports: [RouterOutlet],
  styleUrl: './app.scss',
})
export class App {
  private readonly authStore = inject(AuthStore);
  private readonly themeService = inject(ThemeService);

  protected readonly isStartupLoading = this.authStore.isStartupLoading;

  constructor() {
    this.themeService.synchronize();
    this.authStore.initialize().pipe(takeUntilDestroyed()).subscribe();
  }
}
