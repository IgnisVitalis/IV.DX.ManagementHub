import { DXColumnType, isSystemColumn } from './dx-column-type';
import type { UnitColumn, UnitStructure } from './models/unit-structure';
import { isEditableColumn, isNumericColumn, isSecretColumn } from './unit-record.patch';

/** Which control renders a column. */
export type EditorKind =
  'bool' | 'select' | 'number' | 'text' | 'textarea' | 'datetime' | 'secret' | 'unsupported';

export interface EditorOption {
  readonly value: string;
  readonly label: string;
}

export interface EditorField {
  readonly column: UnitColumn;
  readonly kind: EditorKind;
  /** Choices for `select`; empty otherwise. */
  readonly options: readonly EditorOption[];
  readonly required: boolean;
  readonly maxLength: number | null;
}

function toOptions(lookup: Readonly<Record<string, string>> | null): readonly EditorOption[] {
  return Object.entries(lookup ?? {})
    .map(([value, label]) => ({ value, label }))
    .sort((a, b) => a.label.localeCompare(b.label));
}

function editorKind(column: UnitColumn): EditorKind {
  if (!isEditableColumn(column)) {
    return 'unsupported';
  }

  if (column.type === DXColumnType.Bool) {
    return 'bool';
  }

  if (isSecretColumn(column)) {
    return 'secret';
  }

  // A column backed by an enum or a relation is a choice, not free text.
  if (isNumericColumn(column) && column.enumValues !== null) {
    return 'select';
  }

  if (column.type === DXColumnType.Guid && column.relationValues !== null) {
    return 'select';
  }

  if (column.type === DXColumnType.DateTime) {
    return 'datetime';
  }

  if (isNumericColumn(column)) {
    return 'number';
  }

  return column.type === DXColumnType.Text ? 'textarea' : 'text';
}

function toEditorField(column: UnitColumn): EditorField {
  const kind = editorKind(column);
  const options =
    kind === 'select'
      ? toOptions(column.enumValues !== null ? column.enumValues : column.relationValues)
      : [];

  return {
    column,
    kind,
    options,
    // A checkbox always has a value, and an empty secret means "leave as is",
    // so neither can be required.
    required: !column.allowNull && kind !== 'bool' && kind !== 'secret',
    maxLength: column.length,
  };
}

/** Editable fields of the unit's main element, in metadata order. */
export function toEditorFields(structure: UnitStructure | null): readonly EditorField[] {
  return (structure?.main.columns ?? [])
    .filter((column) => !isSystemColumn(column.name) && isEditableColumn(column))
    .map(toEditorField);
}
