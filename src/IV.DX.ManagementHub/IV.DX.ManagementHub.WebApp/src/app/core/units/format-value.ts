import { DXColumnType } from './dx-column-type';
import type { UnitColumn } from './models/unit-structure';

/** Shown wherever a value is absent. */
export const EMPTY_VALUE = '—';

function formatDateTime(raw: unknown): string {
  const date = new Date(String(raw));

  if (Number.isNaN(date.getTime())) {
    return String(raw);
  }

  // Same shape as the Blazor viewer's "u" format, minus the trailing Z.
  return date
    .toISOString()
    .replace('T', ' ')
    .replace(/\.\d+Z$/, '');
}

/**
 * Renders one raw value as text, following the rules of the Blazor
 * `DXItemViewer`: enum and relation columns show their label, secrets are
 * masked, and anything absent shows a dash.
 */
export function formatValue(column: UnitColumn, raw: unknown): string {
  switch (column.type) {
    case DXColumnType.Bool:
      return raw === true ? 'Yes' : raw === false ? 'No' : EMPTY_VALUE;

    // Never render a secret, even when the API returns one.
    case DXColumnType.HashedString:
    case DXColumnType.EncryptedString:
      return typeof raw === 'string' && raw.length > 0 ? '******' : EMPTY_VALUE;

    case DXColumnType.Blob:
      // Blob download is not ported yet; say so rather than print binary.
      return raw === null || raw === undefined ? EMPTY_VALUE : 'file';

    default:
      break;
  }

  if (raw === null || raw === undefined || raw === '') {
    return EMPTY_VALUE;
  }

  switch (column.type) {
    case DXColumnType.TimeStamp:
    case DXColumnType.DateTime:
      return formatDateTime(raw);

    case DXColumnType.Short:
    case DXColumnType.Int:
      return column.enumValues?.[String(raw)] ?? String(raw);

    case DXColumnType.Guid:
      return column.relationValues?.[String(raw)] ?? String(raw);

    default:
      return typeof raw === 'object' ? JSON.stringify(raw) : String(raw);
  }
}
