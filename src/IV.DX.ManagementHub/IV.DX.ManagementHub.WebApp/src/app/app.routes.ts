import type { Routes } from '@angular/router';

import { Shell } from './core/layout/shell/shell';
import { defaultInstanceGuard, instanceGuard } from './core/instances/instance.guards';

/**
 * Root route table.
 *
 * Every screen lives under `/app/:instanceKey`, so the DX instance the user works
 * against is part of the URL: links carry it, a reload keeps it, and two tabs can
 * show two instances at once. The Blazor UI uses the same shape.
 *
 * Feature route tables are loaded lazily and mounted as children of the shell, so
 * every page inherits the toolbar and navigation.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [defaultInstanceGuard], children: [] },
  {
    path: 'app/:instanceKey',
    component: Shell,
    canActivate: [instanceGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
      },
      {
        path: 'cards',
        loadChildren: () =>
          import('./features/card-view/card-view.routes').then((m) => m.CARD_VIEW_ROUTES),
      },
      {
        path: 'view',
        loadChildren: () =>
          import('./features/dataset-view/dataset-view.routes').then((m) => m.DATASET_VIEW_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
