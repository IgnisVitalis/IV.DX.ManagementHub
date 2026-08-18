import type { Routes } from '@angular/router';

/** Lazily loaded route table of the `dashboard` feature. */
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    title: 'Дашборд',
    loadComponent: () =>
      import('./pages/dashboard-page/dashboard-page').then((m) => m.DashboardPage),
  },
];
