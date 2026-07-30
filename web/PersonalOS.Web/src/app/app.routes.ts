import { Routes } from '@angular/router';

import { anonymousOnlyGuard, authenticatedGuard } from './core/auth/auth.guards';

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
        path: 'settings',
        title: 'Settings - PersonalOS',
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
