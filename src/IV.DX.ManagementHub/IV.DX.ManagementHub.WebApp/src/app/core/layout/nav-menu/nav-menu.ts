import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { APP_CONFIG } from '@core/config/app-config';
import { InstancesService } from '@core/instances/instances.service';
import { Notice } from '@shared/ui/notice/notice';
import { NavigationService } from '@core/navigation/navigation.service';
import { flattenNavTree } from '@core/navigation/nav-tree';
import { navLinkOf } from '@core/navigation/nav-link';
import type { NavItem } from '@core/navigation/models/nav-item';

/** Side navigation built from DX metadata. */
@Component({
  selector: 'mh-nav-menu',
  imports: [
    Notice,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatListModule,
    MatProgressBarModule,
  ],
  templateUrl: './nav-menu.html',
  styleUrl: './nav-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavMenu {
  private readonly navigation = inject(NavigationService).navigation;
  private readonly instances = inject(InstancesService);
  private readonly config = inject(APP_CONFIG);

  /**
   * The instances screen has no navigation metadata pointing at it, so it is
   * linked statically — the Blazor menu does the same.
   *
   * It always opens in the hub's own instance: the card view describing it is
   * hub-owned metadata, and a remote DX instance answers 404 for it. Following
   * this link therefore also switches the instance, which is the honest
   * behaviour — the screen is about the hub, not about the current instance.
   */
  protected readonly instancesLink = computed(
    () => `/app/${this.config.hubInstanceKey}/cards/${this.config.instancesCardViewId}`,
  );
  private readonly expandedIds = signal<ReadonlySet<string>>(new Set());

  protected readonly isLoading = this.navigation.isLoading;
  protected readonly error = this.navigation.error;

  /** Entries whose ancestors are all expanded, in render order. */
  protected readonly rows = computed(() => {
    const key = this.instances.currentKey();
    const linkOf = key === undefined ? () => null : (item: NavItem) => navLinkOf(item, key);

    return flattenNavTree(this.navigation.value(), this.expandedIds(), linkOf);
  });

  protected readonly isEmpty = computed(
    () => !this.isLoading() && !this.error() && this.navigation.value().length === 0,
  );

  protected toggle(id: string): void {
    this.expandedIds.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  }

  protected reload(): void {
    this.navigation.reload();
  }
}
