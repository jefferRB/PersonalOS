import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';

import { AuthStore } from '../auth/auth.store';

/** Implemented by components that can hold unsaved edits. */
export interface UnsavedChangesAware {
  /** Reports whether leaving now would discard edits the user has not saved. */
  hasUnsavedChanges(): boolean;
}

/** Message shown before unsaved edits are discarded. */
export const UNSAVED_CHANGES_MESSAGE =
  'You have unsaved changes. Leave this page and discard them?';

/**
 * Asks for confirmation before navigating away from a form with unsaved edits.
 *
 * A native browser confirmation is used deliberately: it is accessible, needs no modal framework,
 * and is straightforward to test. The guard is a user-experience safeguard only; it protects
 * nothing on the server.
 *
 * The check is skipped once the session has ended, so signing out never asks the user to decide
 * about edits that can no longer be saved.
 */
export const unsavedChangesGuard: CanDeactivateFn<UnsavedChangesAware> = (component) => {
  const authStore = inject(AuthStore);

  if (!authStore.isAuthenticated()) {
    return true;
  }

  return !component.hasUnsavedChanges() || window.confirm(UNSAVED_CHANGES_MESSAGE);
};
