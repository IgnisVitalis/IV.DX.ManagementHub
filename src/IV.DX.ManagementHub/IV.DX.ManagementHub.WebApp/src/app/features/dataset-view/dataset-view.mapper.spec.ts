import { formatCell, toDatasetTable, toDatasetViewDefinition } from './dataset-view.mapper';

/** Real `GET /api/management/DXPDataSetViewUnit/{id}` payload, trimmed. */
const viewPayload = {
  Meta: { Kind: 'DXUnit', Type: 'DXPDataSetViewUnit', IsMulti: true },
  Data: {
    Items: [
      {
        DXElements: { DXPComponentButtonActionElement: { Data: { Items: null } } },
        Id: '018fa54b-aac6-7d9a-9c81-ab6bb4df37a7',
        TimeStamp: '2026-08-11T16:12:19.359978Z',
        DXTitle: 'DXEnum DataSetView',
        IsCreatable: true,
        IsEditable: true,
        IsDeletable: true,
        IsExportable: true,
        DXQuery: '018fa54b-58be-7551-bd5b-e4d9c6c922c4',
        Name: 'DXEnum DataSetView',
        DerivedDXUnitType: '018fa54a-f716-7cff-816f-ca24e1f18cca',
      },
    ],
    Delete: null,
  },
};

/** Real `query-result` payload with an out-of-order and a numeric column. */
const queryPayload = {
  QueryDefinition: [
    { Name: 'Order', Expression: 'Order', Order: 30 },
    { Name: 'Id', Expression: 'Id', Order: -1 },
    { Name: 'Name', Expression: 'Name', Order: 20 },
  ],
  Content: {
    Meta: { Kind: 'DXUnit', Type: 'DXPNavigationItemUnit', IsMulti: true },
    Data: {
      Items: [
        { Id: 'id-1', DXTitle: null, Name: 'Enums', Order: 10 },
        { Id: 'id-2', DXTitle: null, Name: null, Order: 20 },
      ],
    },
  },
};

describe('toDatasetViewDefinition', () => {
  it('reads the unit out of the multi-item envelope', () => {
    expect(toDatasetViewDefinition(viewPayload)).toEqual({
      id: '018fa54b-aac6-7d9a-9c81-ab6bb4df37a7',
      title: 'DXEnum DataSetView',
      queryId: '018fa54b-58be-7551-bd5b-e4d9c6c922c4',
      isCreatable: true,
      isEditable: true,
      isDeletable: true,
      isExportable: true,
    });
  });

  it('reports a unit without a query as unresolved', () => {
    const withoutQuery = {
      ...viewPayload,
      Data: { Items: [{ ...viewPayload.Data.Items[0], DXQuery: null }] },
    };

    expect(toDatasetViewDefinition(withoutQuery)).toBeNull();
  });

  it('returns null instead of throwing on an unexpected payload', () => {
    expect(toDatasetViewDefinition(null)).toBeNull();
    expect(toDatasetViewDefinition({ Data: { Items: [] } })).toBeNull();
    expect(toDatasetViewDefinition({ error: 'unauthorized' })).toBeNull();
  });
});

describe('toDatasetTable', () => {
  it('orders columns by Order and drops the Id column', () => {
    expect(toDatasetTable(queryPayload).columns).toEqual([{ name: 'Name' }, { name: 'Order' }]);
  });

  it('carries the unit type and keeps raw values alongside the rendered text', () => {
    const table = toDatasetTable(queryPayload);

    expect(table.typeName).toBe('DXPNavigationItemUnit');
    expect(table.rows[0]).toEqual({
      id: 'id-1',
      values: { Name: 'Enums', Order: 10 },
      display: { Name: 'Enums', Order: '10' },
    });
  });

  it('renders a null cell as empty text', () => {
    expect(toDatasetTable(queryPayload).rows[1].display['Name']).toBe('');
  });

  it('returns an empty table instead of throwing on an unexpected payload', () => {
    expect(toDatasetTable({ error: 'unauthorized' }).rows).toEqual([]);
    expect(toDatasetTable(undefined).columns).toEqual([]);
  });
});

describe('formatCell', () => {
  it('renders primitives, blanks and objects', () => {
    expect(formatCell('text')).toBe('text');
    expect(formatCell(0)).toBe('0');
    expect(formatCell(false)).toBe('false');
    expect(formatCell(null)).toBe('');
    expect(formatCell(undefined)).toBe('');
    expect(formatCell({ a: 1 })).toBe('{"a":1}');
  });
});
