/**
 * `DXColumnTypeEnum` as stored in DX metadata.
 *
 * The values are not guessed: they come from the `DXColumnTypeEnum` definition
 * the API itself returns in the `EnumValues` map of any column of that type.
 */
export const DXColumnType = {
  Guid: 1,
  TimeStamp: 2,
  String: 3,
  Text: 4,
  DateTime: 5,
  Bool: 6,
  Short: 7,
  Int: 8,
  Long: 9,
  Decimal: 10,
  Float: 11,
  Currency: 12,
  Blob: 13,
  HashedString: 14,
  EncryptedString: 15,
} as const;

export type DXColumnTypeValue = (typeof DXColumnType)[keyof typeof DXColumnType];

/**
 * Bookkeeping columns that carry no information for a reader. Mirrors the list
 * the Blazor viewers hide.
 */
export const SYSTEM_COLUMNS: readonly string[] = ['Id', 'DXUnitId', 'TimeStamp'];

export function isSystemColumn(name: string): boolean {
  return SYSTEM_COLUMNS.some((system) => system.toLowerCase() === name.toLowerCase());
}

/** The element whose rows describe the columns of a DX unit or element. */
export const COLUMN_DEFINITION_ELEMENT = 'DXColumnDefinitionElement';

/**
 * A `DXColumnDefinitionElement` row describing one of DX's own bookkeeping
 * columns (`Id`, `TimeStamp`, `DXUnitId`).
 *
 * DX creates and maintains those itself, so the Blazor viewer and editor both
 * hide them — they are noise a user can neither meaningfully read nor change.
 * The rule is deliberately narrow: it applies to that one element only, not to
 * every collection that happens to have a `Name` column.
 *
 * Hiding is a rendering concern. The rows must stay in the editor's state,
 * because a row left out of the payload is deleted.
 */
export function isSystemColumnDefinitionRow(elementName: string, columnName: unknown): boolean {
  return (
    elementName.toLowerCase() === COLUMN_DEFINITION_ELEMENT.toLowerCase() &&
    typeof columnName === 'string' &&
    isSystemColumn(columnName)
  );
}
