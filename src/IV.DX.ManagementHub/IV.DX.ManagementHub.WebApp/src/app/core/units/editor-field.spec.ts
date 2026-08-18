import { DXColumnType } from './dx-column-type';
import { toEditorFields } from './editor-field';
import { toUnitStructure } from './unit-structure.mapper';

const structure = toUnitStructure({
  Name: 'SampleUnit',
  MainSingleElement: {
    Name: 'SampleUnit',
    Columns: [
      { Name: 'Id', ColumnType: DXColumnType.Guid, EnumValues: null, RelationValues: null },
      {
        Name: 'TimeStamp',
        ColumnType: DXColumnType.TimeStamp,
        EnumValues: null,
        RelationValues: null,
      },
      {
        Name: 'Name',
        ColumnType: DXColumnType.String,
        EnumValues: null,
        RelationValues: null,
        AllowNull: false,
        Length: 100,
      },
      {
        Name: 'Description',
        ColumnType: DXColumnType.Text,
        EnumValues: null,
        RelationValues: null,
      },
      {
        Name: 'IsPublic',
        ColumnType: DXColumnType.Bool,
        EnumValues: null,
        RelationValues: null,
        AllowNull: false,
      },
      {
        Name: 'Effect',
        ColumnType: DXColumnType.Int,
        EnumValues: { '2': 'Deny', '1': 'Allow' },
        RelationValues: null,
      },
      { Name: 'Order', ColumnType: DXColumnType.Int, EnumValues: null, RelationValues: null },
      {
        Name: 'Parent',
        ColumnType: DXColumnType.Guid,
        EnumValues: null,
        RelationValues: { a: 'Alpha' },
      },
      {
        Name: 'Secret',
        ColumnType: DXColumnType.HashedString,
        EnumValues: null,
        RelationValues: null,
        AllowNull: false,
      },
      { Name: 'Payload', ColumnType: DXColumnType.Blob, EnumValues: null, RelationValues: null },
    ],
  },
  RequiredSingleElements: [],
  OptionalSingleElements: [],
  RequiredMultiElements: [],
  OptionalMultiElements: [],
});

const fields = toEditorFields(structure);
const byName = (name: string) => fields.find((f) => f.column.name === name);

describe('toEditorFields', () => {
  it('leaves out system columns and the types the editor cannot handle', () => {
    expect(fields.map((f) => f.column.name)).toEqual([
      'Name',
      'Description',
      'IsPublic',
      'Effect',
      'Order',
      'Parent',
      'Secret',
    ]);
  });

  it('picks a control per column type', () => {
    expect(byName('Name')?.kind).toBe('text');
    expect(byName('Description')?.kind).toBe('textarea');
    expect(byName('IsPublic')?.kind).toBe('bool');
    expect(byName('Order')?.kind).toBe('number');
    expect(byName('Secret')?.kind).toBe('secret');
  });

  it('turns enum and relation columns into choices, sorted by label', () => {
    expect(byName('Effect')?.kind).toBe('select');
    expect(byName('Effect')?.options).toEqual([
      { value: '1', label: 'Allow' },
      { value: '2', label: 'Deny' },
    ]);
    expect(byName('Parent')?.options).toEqual([{ value: 'a', label: 'Alpha' }]);
  });

  it('marks a non-nullable column required, but never a checkbox or a secret', () => {
    expect(byName('Name')).toMatchObject({ required: true, maxLength: 100 });
    expect(byName('IsPublic')?.required).toBe(false);
    expect(byName('Secret')?.required).toBe(false);
    expect(byName('Order')?.required).toBe(false);
  });
});
