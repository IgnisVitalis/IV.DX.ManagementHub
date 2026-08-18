import { filterOptions, type PicklistOption } from './picklist-option';

const options: readonly PicklistOption[] = [
  { value: '018fa545-a3ce', label: 'DXUnitDefinitionUnit' },
  { value: '018fa545-8876', label: 'DXObjectDefinitionUnit' },
  { value: 'c5e6f7a8-9b0c', label: 'MHInstanceUnit' },
];

describe('filterOptions', () => {
  it('returns everything for an empty or blank search', () => {
    expect(filterOptions(options, '')).toBe(options);
    expect(filterOptions(options, '   ')).toBe(options);
  });

  it('matches part of a label regardless of case', () => {
    expect(filterOptions(options, 'object').map((o) => o.label)).toEqual([
      'DXObjectDefinitionUnit',
    ]);
    expect(filterOptions(options, 'UNIT')).toHaveLength(3);
  });

  it('ignores the value, which the user never sees', () => {
    expect(filterOptions(options, 'c5e6f7a8')).toEqual([]);
  });

  it('yields nothing when there is no match', () => {
    expect(filterOptions(options, 'нет такого')).toEqual([]);
  });
});
