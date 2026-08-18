import type { DatasetRow } from './models/dataset-table';
import { sortDatasetRows } from './sort-rows';

function row(id: string, values: Record<string, unknown>): DatasetRow {
  return { id, values, display: {} };
}

const rows = [
  row('a', { name: 'Beta', order: 9 }),
  row('b', { name: 'alpha', order: 10 }),
  row('c', { name: null, order: 1 }),
];

const ids = (result: readonly DatasetRow[]) => result.map((r) => r.id);

describe('sortDatasetRows', () => {
  it('leaves the rows untouched without a direction', () => {
    expect(sortDatasetRows(rows, 'name', '')).toBe(rows);
  });

  it('sorts numbers numerically rather than as text', () => {
    expect(ids(sortDatasetRows(rows, 'order', 'asc'))).toEqual(['c', 'a', 'b']);
    expect(ids(sortDatasetRows(rows, 'order', 'desc'))).toEqual(['b', 'a', 'c']);
  });

  it('sorts text case-insensitively', () => {
    expect(ids(sortDatasetRows(rows, 'name', 'asc')).slice(0, 2)).toEqual(['b', 'a']);
  });

  it('keeps empty values last in both directions', () => {
    expect(ids(sortDatasetRows(rows, 'name', 'asc')).at(-1)).toBe('c');
    expect(ids(sortDatasetRows(rows, 'name', 'desc')).at(-1)).toBe('c');
  });

  it('does not mutate the input', () => {
    const original = [...rows];
    sortDatasetRows(rows, 'order', 'desc');
    expect(rows).toEqual(original);
  });
});
