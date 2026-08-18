import { DXColumnType } from './dx-column-type';
import { EMPTY_VALUE, formatValue } from './format-value';
import type { UnitColumn } from './models/unit-structure';

function column(type: number, extra: Partial<UnitColumn> = {}): UnitColumn {
  return {
    name: 'C',
    type,
    enumValues: null,
    relationValues: null,
    allowNull: true,
    length: null,
    defaultValue: null,
    ...extra,
  };
}

describe('formatValue', () => {
  it('renders booleans as words, including false', () => {
    expect(formatValue(column(DXColumnType.Bool), true)).toBe('Да');
    expect(formatValue(column(DXColumnType.Bool), false)).toBe('Нет');
    expect(formatValue(column(DXColumnType.Bool), null)).toBe(EMPTY_VALUE);
  });

  it('never reveals a secret', () => {
    expect(formatValue(column(DXColumnType.HashedString), 'admin')).toBe('******');
    expect(formatValue(column(DXColumnType.EncryptedString), 'sensitive')).toBe('******');
    expect(formatValue(column(DXColumnType.HashedString), '')).toBe(EMPTY_VALUE);
  });

  it('resolves an enum-backed integer to its label', () => {
    const effect = column(DXColumnType.Int, { enumValues: { '1': 'Allow', '2': 'Deny' } });

    expect(formatValue(effect, 1)).toBe('Allow');
    expect(formatValue(effect, 2)).toBe('Deny');
    // An unmapped value must still be readable rather than blank.
    expect(formatValue(effect, 7)).toBe('7');
  });

  it('resolves a relation guid to the related unit name', () => {
    const relation = column(DXColumnType.Guid, {
      relationValues: { '018fa545-a3ce-7500-aabb-5bbf4767a6b1': 'DXUnitDefinitionUnit' },
    });

    expect(formatValue(relation, '018fa545-a3ce-7500-aabb-5bbf4767a6b1')).toBe(
      'DXUnitDefinitionUnit',
    );
    expect(formatValue(relation, 'c5e6f7a8-9b0c-4d1e-2f3a-4b5c6d7e8f9a')).toBe(
      'c5e6f7a8-9b0c-4d1e-2f3a-4b5c6d7e8f9a',
    );
  });

  it('renders timestamps without the ISO noise', () => {
    expect(formatValue(column(DXColumnType.TimeStamp), '2026-08-11T16:12:19.713468Z')).toBe(
      '2026-08-11 16:12:19',
    );
  });

  it('treats zero and empty string differently', () => {
    expect(formatValue(column(DXColumnType.Int), 0)).toBe('0');
    expect(formatValue(column(DXColumnType.String), '')).toBe(EMPTY_VALUE);
  });

  it('keeps unknown column types readable', () => {
    expect(formatValue(column(999), 'value')).toBe('value');
    expect(formatValue(column(DXColumnType.Blob), null)).toBe(EMPTY_VALUE);
  });
});
