import { Routes } from '@angular/router';

import { anonymousOnlyGuard, authenticatedGuard } from './core/auth/auth.guards';
import { unsavedChangesGuard } from './core/navigation/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'app/today',
  },
  {
    path: 'login',
    title: 'Sign in - PersonalOS',
    canActivate: [anonymousOnlyGuard],
    loadComponent: () =>
      import('./features/authentication/login/login.component').then(
        (module) => module.LoginComponent,
      ),
  },
  {
    path: 'register',
    title: 'Create account - PersonalOS',
    canActivate: [anonymousOnlyGuard],
    loadComponent: () =>
      import('./features/authentication/register/register.component').then(
        (module) => module.RegisterComponent,
      ),
  },
  {
    path: 'app',
    canActivate: [authenticatedGuard],
    loadComponent: () =>
      import('./core/layout/application-shell/application-shell.component').then(
        (module) => module.ApplicationShellComponent,
      ),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'today',
      },
      {
        path: 'today',
        title: 'Today - PersonalOS',
        loadComponent: () =>
          import('./features/today/today.component').then((module) => module.TodayComponent),
      },
      {
        path: 'calendar',
        title: 'Calendar - PersonalOS',
        loadComponent: () =>
          import('./features/calendar/calendar.component').then(
            (module) => module.CalendarComponent,
          ),
      },
      {
        path: 'routines',
        title: 'Routines - PersonalOS',
        loadComponent: () =>
          import('./features/routines/routines.component').then(
            (module) => module.RoutinesComponent,
          ),
      },
      {
        // The identifier is bound to the component's `id` input by the router's component input
        // binding, so the component never has to read the snapshot itself.
        path: 'routines/:id',
        title: 'Routine - PersonalOS',
        loadComponent: () =>
          import('./features/routines/routine-detail.component').then(
            (module) => module.RoutineDetailComponent,
          ),
      },
      {
        path: 'nutrition',
        title: 'Nutrition - PersonalOS',
        loadComponent: () =>
          import('./features/nutrition/nutrition.component').then(
            (module) => module.NutritionComponent,
          ),
      },
      {
        path: 'study',
        title: 'Study - PersonalOS',
        loadComponent: () =>
          import('./features/study/study.component').then((module) => module.StudyComponent),
      },
      {
        path: 'journal',
        title: 'Journal - PersonalOS',
        // A reflection cannot be reconstructed from anywhere else, so leaving with unsaved edits
        // asks for confirmation.
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./features/journal/journal.component').then((module) => module.JournalComponent),
      },
      {
        path: 'settings',
        title: 'Settings - PersonalOS',
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./features/settings/settings.component').then((module) => module.SettingsComponent),
      },
    ],
  },
  {
    path: '**',
    title: 'Page not found - PersonalOS',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component').then(
        (module) => module.NotFoundComponent,
      ),
  },
];
