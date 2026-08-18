import { firstDataBlockItem } from '@core/api/models/dx-data-block';
import { DXColumnType } from './dx-column-type';
import type { UnitColumn } from './models/unit-structure';

/** Value a form control holds for one column. */
export type EditValue = string | number | boolean | null;

const NUMERIC_TYPES: readonly number[] = [
  DXColumnType.Short,
  DXColumnType.Int,
  DXColumnType.Long,
  DXColumnType.Decimal,
  DXColumnType.Float,
  DXColumnType.Currency,
];

const SECRET_TYPES: readonly number[] = [DXColumnType.HashedString, DXColumnType.EncryptedString];

export function isNumericColumn(column: UnitColumn): boolean {
  return NUMERIC_TYPES.includes(column.type);
}

export function isSecretColumn(column: UnitColumn): boolean {
  return SECRET_TYPES.includes(column.type);
}

/** Columns the editor cannot handle yet. */
export function isEditableColumn(column: UnitColumn): boolean {
  return column.type !== DXColumnType.Blob && column.type !== DXColumnType.TimeStamp;
}

/** `datetime-local` wants `YYYY-MM-DDTHH:mm`, not an ISO string with a zone. */
function toLocalInputValue(raw: unknown): string {
  const date = new Date(String(raw));
  return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 16);
}

/** Wire value → the value a form control starts with. */
export function toEditValue(column: UnitColumn, raw: unknown): EditValue {
  if (column.type === DXColumnType.Bool) {
    return raw === true;
  }

  // A secret is never round-tripped: the API redacts it, so the field starts
  // empty and only a value typed by the user is ever sent back.
  if (isSecretColumn(column)) {
    return '';
  }

  if (raw === null || raw === undefined) {
    return isNumericColumn(column) ? null : '';
  }

  if (column.type === DXColumnType.DateTime) {
    return toLocalInputValue(raw);
  }

  return isNumericColumn(column) ? Number(raw) : String(raw);
}

/** Form value → the value to send. */
export function toWireValue(column: UnitColumn, value: EditValue): unknown {
  if (column.type === DXColumnType.Bool) {
    return value === true;
  }

  if (value === null || value === '') {
    return null;
  }

  if (isNumericColumn(column)) {
    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : parsed;
  }

  if (column.type === DXColumnType.DateTime) {
    const date = new Date(String(value));
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  return String(value);
}

/**
 * Returns a copy of the record payload with the main element's columns replaced
 * by the edited values.
 *
 * Patching what the API returned — rather than rebuilding a payload — keeps
 * `Meta` and the whole `DXElements` tree byte-identical, so saving a name cannot
 * quietly drop a collection.
 *
 * An empty secret field is left out entirely: the API redacts those columns on
 * read, so writing the empty string back would erase the stored value.
 */
export function applyEdits(
  rawRecord: unknown,
  columns: readonly UnitColumn[],
  values: Readonly<Record<string, EditValue>>,
): unknown {
  const payload = structuredClone(rawRecord) as {
    Data?: { Items?: Record<string, unknown>[] | null };
  };

  const item = payload?.Data?.Items?.[0];

  if (item === undefined) {
    throw new Error('Запись не содержит данных для сохранения.');
  }

  for (const column of columns) {
    if (!isEditableColumn(column) || !(column.name in values)) {
      continue;
    }

    const value = values[column.name];

    if (isSecretColumn(column) && (value === '' || value === null)) {
      continue;
    }

    item[column.name] = toWireValue(column, value);
  }

  return payload;
}

/**
 * Starting values for a new record, taken from the column defaults.
 *
 * DX stores defaults as text and writes booleans as `'0'` or `'false'`, both of
 * which are truthy strings — they have to be compared, not coerced.
 */
export function toNewEditValues(columns: readonly UnitColumn[]): Record<string, EditValue> {
  const values: Record<string, EditValue> = {};

  for (const column of columns) {
    const fallback = toEditValue(column, null);
    const declared = column.defaultValue;

    if (declared === null) {
      values[column.name] = fallback;
      continue;
    }

    if (column.type === DXColumnType.Bool) {
      values[column.name] = declared === 'true' || declared === '1';
    } else if (isNumericColumn(column)) {
      const parsed = Number(declared);
      values[column.name] = Number.isNaN(parsed) ? null : parsed;
    } else {
      values[column.name] = declared;
    }
  }

  return values;
}

/**
 * Builds the payload for a brand new record.
 *
 * The API generates `Id` and answers 201 with it, so nothing identifying is sent.
 */
export function buildNewRecord(
  typeName: string,
  columns: readonly UnitColumn[],
  values: Readonly<Record<string, EditValue>>,
): unknown {
  const item: Record<string, unknown> = {};

  for (const column of columns) {
    if (!isEditableColumn(column) || !(column.name in values)) {
      continue;
    }

    const value = values[column.name];

    if (isSecretColumn(column) && (value === '' || value === null)) {
      continue;
    }

    item[column.name] = toWireValue(column, value);
  }

  return {
    Meta: { Kind: 'DXUnit', Type: typeName, IsMulti: true },
    Data: { Items: [item] },
  };
}

/** Starting values of the editor, taken from the loaded record. */
export function toEditValues(
  columns: readonly UnitColumn[],
  rawRecord: unknown,
): Record<string, EditValue> {
  const item = firstDataBlockItem<Record<string, unknown>>(rawRecord);
  const values: Record<string, EditValue> = {};

  for (const column of columns) {
    values[column.name] = toEditValue(column, item?.[column.name] ?? null);
  }

  return values;
}
