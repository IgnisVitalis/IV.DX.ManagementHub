/** One navigation entry as stored in DX (`DXPNavigationItemUnit`). */
export interface NavItem {
  readonly id: string;
  readonly name: string;
  readonly parentId: string | null;
  /** Sort order among siblings. */
  readonly order: number;
  /** Unit type of the linked component — dataset view, card view, ... */
  readonly componentType: string | null;
  /** Identifier of the linked component; `null` for pure grouping entries. */
  readonly componentId: string | null;
}

/** A `NavItem` with its children resolved. */
export interface NavTreeNode {
  readonly item: NavItem;
  readonly children: readonly NavTreeNode[];
}
