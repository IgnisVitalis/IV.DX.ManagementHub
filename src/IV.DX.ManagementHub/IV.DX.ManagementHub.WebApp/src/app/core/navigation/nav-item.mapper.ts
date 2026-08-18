import { queryRows, type DXRow } from '@core/api/models/dx-query-result';
import type { NavItem } from './models/nav-item';

/** Row shape of the navigation query (`DXPNavigationItemUnit`). */
export interface NavItemRow extends DXRow {
  readonly Name: string | null;
  readonly ParentId: string | null;
  readonly Order: number | null;
  readonly ComponentType: string | null;
  readonly ComponentId: string | null;
}

/** Maps one DX row onto the domain model, defaulting the nullable columns. */
export function toNavItem(row: NavItemRow): NavItem {
  return {
    id: row.Id,
    name: row.Name ?? '',
    parentId: row.ParentId ?? null,
    order: row.Order ?? 0,
    componentType: row.ComponentType ?? null,
    componentId: row.ComponentId ?? null,
  };
}

/** Maps a raw `query-result` payload onto navigation items. */
export function toNavItems(raw: unknown): readonly NavItem[] {
  return queryRows<NavItemRow>(raw).map(toNavItem);
}
