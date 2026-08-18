import { applyCollectionEdits, toCollectionEdits, visibleRows } from './collection-edit';
import { DXColumnType, isSystemColumnDefinitionRow } from './dx-column-type';
import { toUnitPreview } from './unit-preview.mapper';
import { toUnitStructure } from './unit-structure.mapper';

const structure = toUnitStructure({
  Name: 'DXElementDefinitionUnit',
  MainSingleElement: {
    Name: 'DXElementDefinitionUnit',
    Columns: [
      { Name: 'Name', ColumnType: DXColumnType.String, EnumValues: null, RelationValues: null },
    ],
  },
  RequiredSingleElements: [],
  OptionalSingleElements: [],
  RequiredMultiElements: [],
  OptionalMultiElements: [
    {
      Name: 'DXColumnDefinitionElement',
      Columns: [
        { Name: 'Name', ColumnType: DXColumnType.String, EnumValues: null, RelationValues: null },
        {
          Name: 'ColumnType',
          ColumnType: DXColumnType.Int,
          EnumValues: { '3': 'String' },
          RelationValues: null,
        },
      ],
    },
    {
      // Same column name, different element: the rule must not reach here.
      Name: 'DXObjectEnumElement',
      Columns: [
        { Name: 'Name', ColumnType: DXColumnType.String, EnumValues: null, RelationValues: null },
      ],
    },
  ],
});

const columnRow = (id: string, name: string) => ({
  Id: id,
  DXUnitId: 'unit-1',
  TimeStamp: '2026-08-11T16:12:13.396933Z',
  Name: name,
  ColumnType: 3,
});

const record = {
  Meta: { Type: 'DXElementDefinitionUnit' },
  Data: {
    Items: [
      {
        Id: 'unit-1',
        Name: 'Sample',
        DXElements: {
          DXColumnDefinitionElement: {
            Data: {
              Items: [
                columnRow('c1', 'Id'),
                columnRow('c2', 'TimeStamp'),
                columnRow('c3', 'Test'),
                columnRow('c4', 'DXUnitId'),
              ],
            },
          },
          DXObjectEnumElement: { Data: { Items: [{ Id: 'e1', Name: 'Id' }] } },
        },
      },
    ],
  },
};

describe('isSystemColumnDefinitionRow', () => {
  it('matches DX bookkeeping columns only inside DXColumnDefinitionElement', () => {
    expect(isSystemColumnDefinitionRow('DXColumnDefinitionElement', 'Id')).toBe(true);
    expect(isSystemColumnDefinitionRow('DXColumnDefinitionElement', 'dxunitid')).toBe(true);
    expect(isSystemColumnDefinitionRow('DXColumnDefinitionElement', 'Test')).toBe(false);
    expect(isSystemColumnDefinitionRow('DXObjectEnumElement', 'Id')).toBe(false);
  });
});

describe('preview', () => {
  it('shows only the columns the user actually defined', () => {
    const groups = toUnitPreview(structure, record).groups;
    const columns = groups.find((g) => g.name === 'DXColumnDefinitionElement');
    const enums = groups.find((g) => g.name === 'DXObjectEnumElement');

    expect(columns?.rows.map((row) => row['Name'])).toEqual(['Test']);
    // A row named "Id" in another element is ordinary data and stays visible.
    expect(enums?.rows.map((row) => row['Name'])).toEqual(['Id']);
  });
});

describe('editor', () => {
  const edits = toCollectionEdits(structure, record);
  const element = structure!.optionalMulti[0];

  it('keeps every row in the edit state', () => {
    expect(edits['DXColumnDefinitionElement'].map((row) => row.values['Name'])).toEqual([
      'Id',
      'TimeStamp',
      'Test',
      'DXUnitId',
    ]);
  });

  it('lists only the user-defined ones', () => {
    expect(visibleRows(element, edits['DXColumnDefinitionElement']).map((r) => r.values['Name'])) //
      .toEqual(['Test']);
  });

  it('writes the hidden rows back, since an omitted row is deleted', () => {
    const patched = applyCollectionEdits(record, structure, edits) as typeof record;
    const saved = patched.Data.Items[0].DXElements.DXColumnDefinitionElement.Data.Items;

    expect(saved.map((row) => row.Name)).toEqual(['Id', 'TimeStamp', 'Test', 'DXUnitId']);
    expect(saved[0]).toMatchObject({ Id: 'c1', DXUnitId: 'unit-1' });
  });
});
