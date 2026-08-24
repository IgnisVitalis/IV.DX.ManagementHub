import type { Routes } from '@angular/router';

/** Lazily loaded route table of the `card-view` feature. */
export const CARD_VIEW_ROUTES: Routes = [
  {
    path: ':componentId',
    title: 'Cards',
    loadComponent: () =>
      import('./pages/card-view-page/card-view-page').then((m) => m.CardViewPage),
  },
];
