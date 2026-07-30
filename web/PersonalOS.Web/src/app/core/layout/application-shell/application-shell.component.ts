import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { finalize, take } from 'rxjs';

import { AuthService } from '../../auth/auth.service';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-application-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './application-shell.component.html',
  styleUrl: './application-shell.component.scss',
})
export class ApplicationShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly authStore = inject(AuthStore);

  protected readonly currentUser = this.authStore.currentUser;
  protected readonly isNavigationOpen = signal(false);
  protected readonly isLoggingOut = signal(false);
  protected readonly displayInitials = computed(() => {
    const user = this.currentUser();
    const source = user?.displayName.trim() || user?.email.trim() || 'PO';
    const [first, second] = source.split(/\s+/);

    return `${first?.charAt(0) ?? 'P'}${second?.charAt(0) ?? 'O'}`.toUpperCase();
  });

  protected toggleNavigation(): void {
    this.isNavigationOpen.update((isOpen) => !isOpen);
  }

  protected closeNavigation(): void {
    this.isNavigationOpen.set(false);
  }

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
        next: () => {
          this.router.navigateByUrl('/login');
        },
        error: () => {
          this.router.navigateByUrl('/login');
        },
      });
  }
}
