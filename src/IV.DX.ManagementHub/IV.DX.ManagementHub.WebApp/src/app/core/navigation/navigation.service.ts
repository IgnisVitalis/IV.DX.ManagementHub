import { Injectable, inject } from '@angular/core';
import { httpResource } from '@angular/common/http';

import { APP_CONFIG } from '@core/config/app-config';
import { InstancesService } from '@core/instances/instances.service';
import type { NavTreeNode } from './models/nav-item';
import { toNavItems } from './nav-item.mapper';
import { buildNavForest } from './nav-tree';

/**
 * Loads the navigation tree from DX metadata.
 *
 * The query id is configuration rather than a constant: DX re-seeds these ids,
 * and a stale one silently produces an empty menu.
 */
@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly config = inject(APP_CONFIG);
  private readonly instances = inject(InstancesService);

  readonly navigation = httpResource<readonly NavTreeNode[]>(
    () => {
      const base = this.instances.apiBase();

      return base === undefined
        ? undefined
        : `${base}/DXQueryResult/${this.config.navItemsQueryId}`;
    },
    {
      parse: (raw) => buildNavForest(toNavItems(raw)),
      defaultValue: [],
    },
  );
}
