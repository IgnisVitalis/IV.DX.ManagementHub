/** One label/value pair of a preview group. */
export interface PreviewField {
  readonly name: string;
  readonly value: string;
}

/**
 * One accordion section of the preview.
 *
 * Single and multi elements share one shape rather than forming a discriminated
 * union: Angular's template type checker does not narrow a union across `@if`
 * blocks, and the unused side is simply an empty array.
 */
export interface PreviewGroup {
  readonly name: string;
  readonly kind: 'single' | 'multi';
  /** Required groups (and the main element) open by default. */
  readonly expanded: boolean;
  /** Populated for `single`. */
  readonly fields: readonly PreviewField[];
  /** Populated for `multi`: column names, and one record of values per row. */
  readonly columns: readonly string[];
  readonly rows: readonly Readonly<Record<string, string>>[];
  /** The element carries no data at all. */
  readonly isEmpty: boolean;
}

export interface UnitPreview {
  readonly typeName: string;
  readonly groups: readonly PreviewGroup[];
}

export const EMPTY_UNIT_PREVIEW: UnitPreview = { typeName: '', groups: [] };
