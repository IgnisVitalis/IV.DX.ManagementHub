import type { NavItem, NavTreeNode } from './models/nav-item';
import type { NavRow } from './models/nav-row';

/** Siblings are ordered by `order`, then by id so the result is stable. */
function compare(a: NavTreeNode, b: NavTreeNode): number {
  return a.item.order - b.item.order || a.item.id.localeCompare(b.item.id);
}

/**
 * Builds the navigation forest from the flat list returned by DX.
 *
 * An entry whose parent is missing from the list is treated as a root, matching
 * the `TreatAsRoot` orphan policy of the Blazor implementation: a menu with a
 * misplaced entry is still usable, a menu that swallowed it is not. Duplicate
 * ids keep the first occurrence for the same reason.
 */
export function buildNavForest(items: readonly NavItem[]): readonly NavTreeNode[] {
  const children = new Map<string, NavTreeNode[]>();
  const nodes = new Map<string, NavTreeNode>();

  for (const item of items) {
    if (nodes.has(item.id)) {
      continue;
    }

    const node: NavTreeNode = { item, children: [] };
    nodes.set(item.id, node);
    children.set(item.id, node.children as NavTreeNode[]);
  }

  const roots: NavTreeNode[] = [];

  for (const node of nodes.values()) {
    const siblings =
      node.item.parentId === null ? roots : (children.get(node.item.parentId) ?? roots);

    siblings.push(node);
  }

  roots.sort(compare);
  for (const list of children.values()) {
    list.sort(compare);
  }

  return roots;
}

/**
 * Flattens the forest to the rows that are currently visible: a node is shown
 * when every one of its ancestors is expanded.
 */
export function flattenNavTree(
  roots: readonly NavTreeNode[],
  expandedIds: ReadonlySet<string>,
  linkOf: (item: NavItem) => string | null,
  level = 0,
): readonly NavRow[] {
  const rows: NavRow[] = [];

  for (const node of roots) {
    const hasChildren = node.children.length > 0;
    const expanded = hasChildren && expandedIds.has(node.item.id);

    rows.push({
      id: node.item.id,
      name: node.item.name,
      level,
      hasChildren,
      expanded,
      link: linkOf(node.item),
    });

    if (expanded) {
      rows.push(...flattenNavTree(node.children, expandedIds, linkOf, level + 1));
    }
  }

  return rows;
}
