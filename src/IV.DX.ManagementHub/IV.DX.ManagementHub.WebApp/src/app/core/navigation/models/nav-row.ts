/**
 * A single rendered line of the menu: the tree flattened down to the entries
 * that are currently visible. Flattening keeps the menu component free of
 * recursion and makes the expand/collapse logic directly testable.
 */
export interface NavRow {
  readonly id: string;
  readonly name: string;
  /** Nesting depth, 0 for roots. */
  readonly level: number;
  readonly hasChildren: boolean;
  readonly expanded: boolean;
  /** Router link, or `null` when the entry has no component to open. */
  readonly link: string | null;
}
