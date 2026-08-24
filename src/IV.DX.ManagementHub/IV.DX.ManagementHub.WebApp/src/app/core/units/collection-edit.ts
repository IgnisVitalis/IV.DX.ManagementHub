import { firstDataBlockItem } from '@core/api/models/dx-data-block';
import { isSystemColumn, isSystemColumnDefinitionRow } from './dx-column-type';
import { formatValue } from './format-value';
import type { UnitColumn, UnitElement, UnitStructure } from './models/unit-structure';
import {
  isEditableColumn,
  isSecretColumn,
  toEditValue,
  toNewEditValues,
  toWireValue,
  type EditValue,
} from './unit-record.patch';

/** One editable row of a collection. */
export interface CollectionRow {
  readonly id: string;
  readonly values: Readonly<Record<string, EditValue>>;
}

/** Rows of every collection, keyed by element name. */
export type CollectionEdits = Readonly<Record<string, readonly CollectionRow[]>>;

type RawRow = Record<string, unknown>;

/** Collections of a unit, required first — the order the editor renders them in. */
export function collectionElements(structure: UnitStructure | null): readonly UnitElement[] {
  return [...(structure?.requiredMulti ?? []), ...(structure?.optionalMulti ?? [])];
}

/** Columns of a collection the user can actually edit. */
export function editableColumns(element: UnitElement): readonly UnitColumn[] {
  return element.columns.filter(
    (column) => !isSystemColumn(column.name) && isEditableColumn(column),
  );
}

function rawRows(rawRecord: unknown, elementName: string): readonly RawRow[] {
  const record = firstDataBlockItem<{
    DXElements?: Record<string, { Data?: { Items?: readonly RawRow[] | null } }> | null;
  }>(rawRecord);

  return record?.DXElements?.[elementName]?.Data?.Items ?? [];
}

/** Current rows of every collection, ready to edit. */
export function toCollectionEdits(
  structure: UnitStructure | null,
  rawRecord: unknown,
): CollectionEdits {
  const edits: Record<string, CollectionRow[]> = {};

  for (const element of collectionElements(structure)) {
    const columns = editableColumns(element);

    edits[element.name] = rawRows(rawRecord, element.name).map((row) => {
      const values: Record<string, EditValue> = {};

      for (const column of columns) {
        values[column.name] = toEditValue(column, row[column.name] ?? null);
      }

      return { id: String(row['Id'] ?? crypto.randomUUID()), values };
    });
  }

  return edits;
}

/**
 * A blank row.
 *
 * The identifier is generated here because the API does not: a row sent without
 * one is stored with an empty GUID on update. The parent keys (`DXUnitId` and
 * friends) are the server's job and must not be invented here.
 */
export function newCollectionRow(element: UnitElement): CollectionRow {
  return { id: crypto.randomUUID(), values: toNewEditValues(editableColumns(element)) };
}

/**
 * Rows worth showing. The hidden ones stay in the edit state on purpose: the API
 * deletes any row missing from the payload, so filtering them out of the state
 * would destroy DX's own column definitions on the next save.
 */
export function visibleRows(
  element: UnitElement,
  rows: readonly CollectionRow[],
): readonly CollectionRow[] {
  return rows.filter((row) => !isSystemColumnDefinitionRow(element.name, row.values['Name']));
}

/** Short summary of a row for the collapsed list. */
export function rowLabel(element: UnitElement, row: CollectionRow): string {
  const label = editableColumns(element)
    .map((column) => formatValue(column, row.values[column.name] ?? null))
    .filter((text) => text !== '' && text !== '—')
    .slice(0, 3)
    .join(' · ');

  return label === '' ? 'Untitled' : label;
}

/** Turns edited rows into wire rows, keeping fields the editor never showed. */
function toRawRows(
  element: UnitElement,
  rows: readonly CollectionRow[],
  existing: readonly RawRow[],
): readonly RawRow[] {
  const columns = editableColumns(element);
  const byId = new Map(existing.map((row) => [String(row['Id']), row]));

  return rows.map((row) => {
    // Keep TimeStamp, DXTitle and the parent keys of a row that already exists.
    const raw: RawRow = { ...(byId.get(row.id) ?? {}), Id: row.id };

    for (const column of columns) {
      const value = row.values[column.name] ?? null;

      if (isSecretColumn(column) && (value === '' || value === null)) {
        continue;
      }

      raw[column.name] = toWireValue(column, value);
    }

    return raw;
  });
}

function elementBlock(element: UnitElement, rows: readonly RawRow[]): unknown {
  return {
    Meta: { Kind: 'DXElement', Type: element.name, IsMulti: true },
    Data: { Items: rows },
  };
}

/**
 * Writes the edited collections into a record payload.
 *
 * Removal is expressed by leaving the row out: the API replaces the collection
 * with what it receives.
 */
export function applyCollectionEdits(
  payload: unknown,
  structure: UnitStructure | null,
  edits: CollectionEdits,
): unknown {
  const elements = collectionElements(structure);

  if (elements.length === 0) {
    return payload;
  }

  const cloned = structuredClone(payload) as {
    Data?: { Items?: (RawRow & { DXElements?: Record<string, unknown> })[] | null };
  };

  const item = cloned?.Data?.Items?.[0];

  if (item === undefined) {
    throw new Error('The record carries no data to save.');
  }

  const existingElements = (item.DXElements ?? {}) as Record<
    string,
    { Data?: { Items?: readonly RawRow[] | null } }
  >;
  const nextElements: Record<string, unknown> = { ...existingElements };

  for (const element of elements) {
    const rows = edits[element.name] ?? [];
    const existing = existingElements[element.name]?.Data?.Items ?? [];

    nextElements[element.name] = elementBlock(element, toRawRows(element, rows, existing));
  }

  item.DXElements = nextElements;

  return cloned;
}
