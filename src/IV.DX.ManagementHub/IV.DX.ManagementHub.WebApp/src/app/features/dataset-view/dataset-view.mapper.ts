import { firstDataBlockItem } from '@core/api/models/dx-data-block';
import { isDXQueryResult, type DXRow } from '@core/api/models/dx-query-result';
import type { DatasetViewDefinition } from './models/dataset-view-definition';
import {
  EMPTY_DATASET_TABLE,
  type DatasetColumn,
  type DatasetRow,
  type DatasetTable,
} from './models/dataset-table';

/** Wire shape of a `DXPDataSetViewUnit`. */
interface DatasetViewRow {
  readonly Id: string;
  readonly Name: string | null;
  readonly DXTitle: string | null;
  readonly DXQuery: string | null;
  readonly IsCreatable: boolean | null;
  readonly IsEditable: boolean | null;
  readonly IsDeletable: boolean | null;
  readonly IsExportable: boolean | null;
}

/**
 * Maps a `DXPDataSetViewUnit` payload onto the view definition.
 *
 * Returns `null` when the unit is missing or carries no query — without a query
 * there is no table to build, and that has to surface as an error rather than as
 * an empty screen.
 */
export function toDatasetViewDefinition(raw: unknown): DatasetViewDefinition | null {
  const unit = firstDataBlockItem<DatasetViewRow>(raw);

  if (unit === null || !unit.DXQuery) {
    return null;
  }

  return {
    id: unit.Id,
    title: unit.Name ?? unit.DXTitle ?? '',
    queryId: unit.DXQuery,
    isCreatable: unit.IsCreatable === true,
    isEditable: unit.IsEditable === true,
    isDeletable: unit.IsDeletable === true,
    isExportable: unit.IsExportable === true,
  };
}

/** Renders one raw cell value as text. */
export function formatCell(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }

  if (typeof value === 'object') {
    return JSON.stringify(value);
  }

  return String(value);
}

/**
 * Maps a `query-result` payload onto the table.
 *
 * Columns come from `QueryDefinition` ordered by `Order`, minus the `Id` column:
 * the identifier addresses a row, it is not information the user reads.
 */
export function toDatasetTable(raw: unknown): DatasetTable {
  if (!isDXQueryResult(raw)) {
    return EMPTY_DATASET_TABLE;
  }

  const columns: readonly DatasetColumn[] = [...raw.QueryDefinition]
    .sort((a, b) => a.Order - b.Order)
    .filter((column) => column.Name !== 'Id')
    .map((column) => ({ name: column.Name }));

  const rows: readonly DatasetRow[] = raw.Content.Data.Items.map((row: DXRow) => {
    const values: Record<string, unknown> = {};
    const display: Record<string, string> = {};

    for (const column of columns) {
      const value = row[column.name] ?? null;
      values[column.name] = value;
      display[column.name] = formatCell(value);
    }

    return { id: row.Id, values, display };
  });

  return { typeName: raw.Content.Meta?.Type ?? '', columns, rows };
}
