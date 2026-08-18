import type { NavItem } from './models/nav-item';

/**
 * Router link a navigation entry opens, or `null` for grouping-only entries.
 *
 * The Blazor version picked the route from `componentType`, comparing it against
 * two hardcoded definition ids. Neither id matches what the database actually
 * returns today, so every entry fell through to the dataset-view route. Until
 * the views themselves are ported the component id alone identifies the target,
 * and `componentType` is carried in the model for the mapping to be added then.
 */
export function navLinkOf(item: NavItem, instanceKey: string): string | null {
  return item.componentId === null ? null : `/app/${instanceKey}/view/${item.componentId}`;
}
