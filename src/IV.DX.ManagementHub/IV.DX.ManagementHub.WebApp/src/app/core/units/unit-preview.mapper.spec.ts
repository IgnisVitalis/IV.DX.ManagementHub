import { toUnitPreview } from './unit-preview.mapper';
import { toUnitStructure } from './unit-structure.mapper';

/** Real `unit-structure/DXRoleUnit`, trimmed to the fields the mapper reads. */
const structurePayload = {
  Name: 'DXRoleUnit',
  MainSingleElement: {
    Name: 'DXRoleUnit',
    Columns: [
      { Name: 'Id', ColumnType: 1, EnumValues: null, RelationValues: null },
      { Name: 'TimeStamp', ColumnType: 2, EnumValues: null, RelationValues: null },
      { Name: 'Name', ColumnType: 3, EnumValues: null, RelationValues: null },
    ],
  },
  RequiredSingleElements: [],
  OptionalSingleElements: [{ Name: 'EmptyElement', Columns: [] }],
  RequiredMultiElements: [],
  OptionalMultiElements: [
    {
      Name: 'DXUnitGrantElement',
      Columns: [
        { Name: 'DXUnitId', ColumnType: 1, EnumValues: null, RelationValues: null },
        { Name: 'Read', ColumnType: 6, EnumValues: null, RelationValues: null },
        {
          Name: 'Effect',
          ColumnType: 8,
          EnumValues: { '1': 'Allow', '2': 'Deny' },
          RelationValues: null,
        },
        {
          Name: 'DXUnit',
          ColumnType: 1,
          EnumValues: null,
          RelationValues: { 'c5e6f7a8-9b0c-4d1e-2f3a-4b5c6d7e8f9a': 'MHInstanceUnit' },
        },
      ],
    },
  ],
};

/** Real `GET /api/management/DXRoleUnit/{id}`, trimmed. */
const recordPayload = {
  Meta: { Kind: 'DXUnit', Type: 'DXRoleUnit' },
  Data: {
    Items: [
      {
        DXElements: {
          DXUnitGrantElement: {
            Meta: { Type: 'DXUnitGrantElement' },
            Data: {
              Items: [
                {
                  DXUnitId: 'c3e4f5a6-7b8c-4d9e-0f1a-2b3c4d5e6f7a',
                  Id: 'd4f5a6b7-8c9d-4e0f-1a2b-3c4d5e6f7a8b',
                  Read: true,
                  DXUnit: 'c5e6f7a8-9b0c-4d1e-2f3a-4b5c6d7e8f9a',
                  Effect: 1,
                },
              ],
            },
          },
        },
        Id: 'c3e4f5a6-7b8c-4d9e-0f1a-2b3c4d5e6f7a',
        TimeStamp: '2026-08-11T16:12:19.713468Z',
        DXTitle: 'MH Instance Manager',
        Name: 'MH Instance Manager',
      },
    ],
  },
};

const structure = toUnitStructure(structurePayload);

describe('toUnitStructure', () => {
  it('drops elements that have no columns', () => {
    expect(structure?.optionalSingle).toEqual([]);
    expect(structure?.optionalMulti.map((e) => e.name)).toEqual(['DXUnitGrantElement']);
  });

  it('returns null for a payload that is not a structure', () => {
    expect(toUnitStructure(null)).toBeNull();
    expect(toUnitStructure({ title: 'Not Found', status: 404 })).toBeNull();
  });
});

describe('toUnitPreview', () => {
  const preview = toUnitPreview(structure, recordPayload);

  it('puts the main element first and expands it', () => {
    expect(preview.typeName).toBe('DXRoleUnit');
    expect(preview.groups[0]).toMatchObject({ name: 'DXRoleUnit', kind: 'single', expanded: true });
  });

  it('hides the bookkeeping columns', () => {
    expect(preview.groups[0].fields.map((f) => f.name)).toEqual(['Name']);
  });

  it('collapses optional groups', () => {
    expect(preview.groups[1]).toMatchObject({ name: 'DXUnitGrantElement', expanded: false });
  });

  it('renders a multi element row through the column formatters', () => {
    const grants = preview.groups[1];

    expect(grants.columns).toEqual(['Read', 'Effect', 'DXUnit']);
    expect(grants.rows).toEqual([{ Read: 'Yes', Effect: 'Allow', DXUnit: 'MHInstanceUnit' }]);
    expect(grants.isEmpty).toBe(false);
  });

  it('marks an element with no items as empty', () => {
    const withoutGrants = toUnitPreview(structure, {
      Meta: { Type: 'DXRoleUnit' },
      Data: { Items: [{ Name: 'Role' }] },
    });

    expect(withoutGrants.groups[1]).toMatchObject({ isEmpty: true, rows: [] });
  });

  it('survives a missing record without throwing', () => {
    const preview = toUnitPreview(structure, null);

    expect(preview.groups[0].fields).toEqual([{ name: 'Name', value: '—' }]);
    expect(preview.groups[0].isEmpty).toBe(true);
  });

  it('returns nothing to render without a structure', () => {
    expect(toUnitPreview(null, recordPayload).groups).toEqual([]);
  });
});
