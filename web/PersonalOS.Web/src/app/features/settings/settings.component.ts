import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, take } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { AuthStore } from '../../core/auth/auth.store';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  private readonly authService = inject(AuthService);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly currentUser = this.authStore.currentUser;
  protected readonly isLoggingOut = signal(false);

  protected logout(): void {
    if (this.isLoggingOut()) {
      return;
    }

    this.isLoggingOut.set(true);

    this.authService
      .logout()
      .pipe(
        take(1),
        finalize(() => this.isLoggingOut.set(false)),
      )
      .subscribe({
        next: () => this.router.navigateByUrl('/login'),
        error: () => this.router.navigateByUrl('/login'),
      });
  }
}
