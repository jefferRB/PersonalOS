import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { CurrentUser } from '../auth/auth.models';
import { AuthStore } from '../auth/auth.store';
import {
  UNSAVED_CHANGES_MESSAGE,
  UnsavedChangesAware,
  unsavedChangesGuard,
} from './unsaved-changes.guard';

describe('unsavedChangesGuard', () => {
  let injector: EnvironmentInjector;
  let store: AuthStore;

  const user: CurrentUser = {
    id: '8d241a6f-9a79-4d2f-83a4-1377c6d56f52',
    displayName: 'Jefferson',
    email: 'jefferson@example.com',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });

    injector = TestBed.inject(EnvironmentInjector);
    store = TestBed.inject(AuthStore);
    store.setAuthenticated(user);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('allows navigation when there is nothing unsaved', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    expect(run(component(false))).toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();
  });

  it('allows navigation when the user confirms discarding changes', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    expect(run(component(true))).toBe(true);
    expect(confirmSpy).toHaveBeenCalledWith(UNSAVED_CHANGES_MESSAGE);
  });

  it('blocks navigation when the user cancels', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    expect(run(component(true))).toBe(false);
  });

  it('does not ask once the session has ended', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);
    store.clearPrivateState();

    expect(run(component(true))).toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();
  });

  function component(hasUnsavedChanges: boolean): UnsavedChangesAware {
    return { hasUnsavedChanges: () => hasUnsavedChanges };
  }

  function run(target: UnsavedChangesAware): boolean | UrlTree {
    return runInInjectionContext(injector, () =>
      unsavedChangesGuard(
        target,
        {} as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
        {} as RouterStateSnapshot,
      ),
    ) as boolean | UrlTree;
  }
});
