import type { DatasetRow } from './models/dataset-table';

function isEmpty(value: unknown): boolean {
  return value === null || value === undefined || value === '';
}

/** Orders two non-empty raw cell values. */
function compareValues(a: unknown, b: unknown): number {
  if (typeof a === 'number' && typeof b === 'number') {
    return a - b;
  }

  // `numeric` keeps "Item 10" after "Item 9" for the mixed text DX columns hold.
  return String(a).localeCompare(String(b), undefined, { numeric: true });
}

/**
 * Sorts rows by one column.
 *
 * Sorting works on the raw values rather than on the rendered text, so numeric
 * columns order numerically. Empty cells always sink to the bottom — they carry
 * no information, and floating them to the top on every `desc` click buries the
 * rows the user is looking at. That is why the direction factor is applied only
 * once both values are known to be present.
 */
export function sortDatasetRows(
  rows: readonly DatasetRow[],
  column: string,
  direction: 'asc' | 'desc' | '',
): readonly DatasetRow[] {
  if (direction === '') {
    return rows;
  }

  const factor = direction === 'asc' ? 1 : -1;

  return [...rows].sort((a, b) => {
    const left = a.values[column];
    const right = b.values[column];

    if (isEmpty(left) || isEmpty(right)) {
      if (isEmpty(left) && isEmpty(right)) {
        return 0;
      }

      return isEmpty(left) ? 1 : -1;
    }

    return factor * compareValues(left, right);
  });
}
