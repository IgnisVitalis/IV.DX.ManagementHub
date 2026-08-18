/**
 * Wire contract of `GET /api/i/{instanceKey}/DXQueryResult/{queryId}`.
 *
 * Property names are PascalCase because they come straight from DX; everything
 * past this file uses camelCase domain models.
 */
export interface DXQueryColumn {
  readonly Name: string;
  readonly Expression: string;
  readonly Order: number;
}

export interface DXQueryResult<TRow> {
  readonly QueryDefinition: readonly DXQueryColumn[];
  readonly Content: {
    /** `Type` is the DX unit type the rows belong to. */
    readonly Meta?: { readonly Type?: string };
    readonly Data: {
      readonly Items: readonly TRow[];
    };
  };
}

/**
 * Every DX row carries at least an identifier. The remaining columns depend on
 * the query, so they are only reachable by name.
 */
export interface DXRow {
  readonly Id: string;
  readonly [column: string]: unknown;
}

/**
 * Narrows an untyped response to a query result. Only the shape actually read
 * downstream is checked — DX adds fields freely and we must not break on them.
 */
export function isDXQueryResult(value: unknown): value is DXQueryResult<DXRow> {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const items = (value as DXQueryResult<DXRow>).Content?.Data?.Items;

  return Array.isArray(items);
}

/** Rows of a query result, or an empty list when the payload is not a query result. */
export function queryRows<TRow extends DXRow>(value: unknown): readonly TRow[] {
  return isDXQueryResult(value) ? (value.Content.Data.Items as readonly TRow[]) : [];
}
