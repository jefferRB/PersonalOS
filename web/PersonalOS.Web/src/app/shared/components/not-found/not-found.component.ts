import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthStore } from '../../../core/auth/auth.store';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.scss',
})
export class NotFoundComponent {
  private readonly authStore = inject(AuthStore);

  protected readonly isAuthenticated = this.authStore.isAuthenticated;
}
