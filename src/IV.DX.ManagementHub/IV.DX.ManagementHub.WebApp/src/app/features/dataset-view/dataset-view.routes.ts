import type { Routes } from '@angular/router';

/**
 * Lazily loaded route table of the `dataset-view` feature.
 *
 * The component id lives here rather than in the parent route so the page's
 * `ActivatedRoute` carries it directly.
 */
export const DATASET_VIEW_ROUTES: Routes = [
  {
    path: ':componentId',
    title: 'Dataset',
    loadComponent: () =>
      import('./pages/dataset-view-page/dataset-view-page').then((m) => m.DatasetViewPage),
  },
];
