/** One column of the table, taken from the query definition. */
export interface DatasetColumn {
  /** Column key, also used as the header label. */
  readonly name: string;
}

export interface DatasetRow {
  readonly id: string;
  /** Raw values, kept for sorting. */
  readonly values: Readonly<Record<string, unknown>>;
  /** Values rendered as text. */
  readonly display: Readonly<Record<string, string>>;
}

export interface DatasetTable {
  /** DX unit type the rows belong to, e.g. `DXEnumDefinitionUnit`. */
  readonly typeName: string;
  readonly columns: readonly DatasetColumn[];
  readonly rows: readonly DatasetRow[];
}

export const EMPTY_DATASET_TABLE: DatasetTable = {
  typeName: '',
  columns: [],
  rows: [],
};
