import {
  applyCollectionEdits,
  collectionElements,
  newCollectionRow,
  rowLabel,
  toCollectionEdits,
} from './collection-edit';
import { DXColumnType } from './dx-column-type';
import { toUnitStructure } from './unit-structure.mapper';

const structure = toUnitStructure({
  Name: 'DXRoleUnit',
  MainSingleElement: {
    Name: 'DXRoleUnit',
    Columns: [
      { Name: 'Name', ColumnType: DXColumnType.String, EnumValues: null, RelationValues: null },
    ],
  },
  RequiredSingleElements: [],
  OptionalSingleElements: [],
  RequiredMultiElements: [],
  OptionalMultiElements: [
    {
      Name: 'DXUnitGrantElement',
      Columns: [
        { Name: 'DXUnitId', ColumnType: DXColumnType.Guid, EnumValues: null, RelationValues: null },
        {
          Name: 'Read',
          ColumnType: DXColumnType.Bool,
          EnumValues: null,
          RelationValues: null,
          DefaultValue: '0',
        },
        {
          Name: 'Effect',
          ColumnType: DXColumnType.Int,
          EnumValues: { '1': 'Allow' },
          RelationValues: null,
        },
      ],
    },
  ],
});

const grants = collectionElements(structure)[0];

/** Real payload: rows carry parent keys and timestamps the editor never shows. */
const record = {
  Meta: { Kind: 'DXUnit', Type: 'DXRoleUnit' },
  Data: {
    Items: [
      {
        Id: 'role-1',
        Name: 'Role',
        DXElements: {
          DXUnitGrantElement: {
            Meta: { Type: 'DXUnitGrantElement' },
            Data: {
              Items: [
                {
                  Id: 'grant-1',
                  DXUnitId: 'role-1',
                  DXRoleUnitId: 'role-1',
                  TimeStamp: '2026-08-11T16:12:19.716862Z',
                  Read: true,
                  Effect: 1,
                },
              ],
            },
          },
        },
      },
    ],
  },
};

const itemsOf = (payload: unknown) =>
  (payload as typeof record).Data.Items[0].DXElements.DXUnitGrantElement.Data.Items;

describe('toCollectionEdits', () => {
  it('turns stored rows into editable ones, keyed by element', () => {
    const edits = toCollectionEdits(structure, record);

    expect(edits['DXUnitGrantElement']).toEqual([
      { id: 'grant-1', values: { Read: true, Effect: 1 } },
    ]);
  });

  it('yields an empty list when the record has no such element', () => {
    expect(toCollectionEdits(structure, { Data: { Items: [{ Id: 'x' }] } })).toEqual({
      DXUnitGrantElement: [],
    });
  });
});

describe('newCollectionRow', () => {
  it('generates an id, because the API stores an empty GUID without one', () => {
    const row = newCollectionRow(grants);

    expect(row.id).toMatch(/^[0-9a-f-]{36}$/i);
    expect(row.values).toEqual({ Read: false, Effect: null });
  });
});

describe('rowLabel', () => {
  it('summarises a row through the column formatters', () => {
    expect(rowLabel(grants, { id: 'g', values: { Read: true, Effect: 1 } })).toBe('Yes · Allow');
  });

  it('falls back when every value is empty', () => {
    expect(rowLabel(grants, { id: 'g', values: { Read: null, Effect: null } })).toBe('Untitled');
  });
});

describe('applyCollectionEdits', () => {
  it('keeps the parent keys and timestamp of a row that already existed', () => {
    const patched = applyCollectionEdits(record, structure, {
      DXUnitGrantElement: [{ id: 'grant-1', values: { Read: false, Effect: 1 } }],
    });

    expect(itemsOf(patched)[0]).toMatchObject({
      Id: 'grant-1',
      DXUnitId: 'role-1',
      DXRoleUnitId: 'role-1',
      TimeStamp: '2026-08-11T16:12:19.716862Z',
      Read: false,
    });
  });

  it('adds a new row carrying only its id and the edited values', () => {
    const patched = applyCollectionEdits(record, structure, {
      DXUnitGrantElement: [
        { id: 'grant-1', values: { Read: true, Effect: 1 } },
        { id: 'grant-2', values: { Read: true, Effect: 1 } },
      ],
    });

    // The server fills the parent keys itself; inventing them here would be wrong.
    expect(itemsOf(patched)[1]).toEqual({ Id: 'grant-2', Read: true, Effect: 1 });
  });

  it('removes a row by leaving it out', () => {
    const patched = applyCollectionEdits(record, structure, { DXUnitGrantElement: [] });

    expect(itemsOf(patched)).toEqual([]);
  });

  it('does not mutate the loaded record', () => {
    const before = JSON.stringify(record);
    applyCollectionEdits(record, structure, { DXUnitGrantElement: [] });
    expect(JSON.stringify(record)).toBe(before);
  });

  it('creates the element block when the record never had one', () => {
    const bare = { Meta: { Type: 'DXRoleUnit' }, Data: { Items: [{ Id: 'role-2', Name: 'New' }] } };

    const patched = applyCollectionEdits(bare, structure, {
      DXUnitGrantElement: [{ id: 'grant-9', values: { Read: true, Effect: 1 } }],
    });

    expect(itemsOf(patched)).toEqual([{ Id: 'grant-9', Read: true, Effect: 1 }]);
  });
});
