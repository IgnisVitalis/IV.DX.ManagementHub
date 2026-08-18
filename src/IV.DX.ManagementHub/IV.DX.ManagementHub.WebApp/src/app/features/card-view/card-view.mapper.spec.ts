import { toCardSet, toCardViewDefinition } from './card-view.mapper';

/** Real `GET /api/management/DXPCardViewUnit/{id}` payload, trimmed. */
const definitionPayload = {
  Meta: { Kind: 'DXUnit', Type: 'DXPCardViewUnit', IsMulti: true },
  Data: {
    Items: [
      {
        DXElements: {},
        Id: 'f0b1c2d3-4e5f-4a6b-7c8d-9e0f1a2b3c4d',
        DXTitle: 'Instances',
        Name: 'Instances',
        IsCreatable: true,
        IsEditable: true,
        IsDeletable: true,
        IsExportable: true,
        DXPClickAction: '7a8b9c0d-1e2f-4a3b-5c4d-6e7f8a9b0c1d',
        DXUnitDefinition: 'c5e6f7a8-9b0c-4d1e-2f3a-4b5c6d7e8f9a',
      },
    ],
  },
};

/** Real records payload of the unit definition, trimmed. */
const recordsPayload = {
  Meta: { Kind: 'DXUnit', Type: 'MHInstanceUnit', IsMulti: true },
  Data: {
    Items: [
      { Id: '18ef7549-1111-2222-3333-444444444444', DXTitle: 'Own', Key: 'Own' },
      { Id: '18ef7549-5555-6666-7777-888888888888', DXTitle: null, Key: 'Other' },
      { Id: '18ef7549-9999-aaaa-bbbb-cccccccccccc', DXTitle: '   ', Key: 'Blank' },
    ],
  },
};

describe('toCardViewDefinition', () => {
  it('reads the unit definition the cards come from', () => {
    expect(toCardViewDefinition(definitionPayload)).toEqual({
      id: 'f0b1c2d3-4e5f-4a6b-7c8d-9e0f1a2b3c4d',
      title: 'Instances',
      unitDefinitionId: 'c5e6f7a8-9b0c-4d1e-2f3a-4b5c6d7e8f9a',
      isCreatable: true,
      isEditable: true,
      isDeletable: true,
      isExportable: true,
    });
  });

  it('reports a definition without a unit definition as unresolved', () => {
    const without = {
      ...definitionPayload,
      Data: { Items: [{ ...definitionPayload.Data.Items[0], DXUnitDefinition: null }] },
    };

    expect(toCardViewDefinition(without)).toBeNull();
    expect(toCardViewDefinition(null)).toBeNull();
    expect(toCardViewDefinition({ title: 'Not Found' })).toBeNull();
  });
});

describe('toCardSet', () => {
  it('carries the unit type and titles the cards by DXTitle', () => {
    const cards = toCardSet(recordsPayload);

    expect(cards.typeName).toBe('MHInstanceUnit');
    expect(cards.items[0]).toEqual({
      id: '18ef7549-1111-2222-3333-444444444444',
      title: 'Own',
    });
  });

  it('falls back to the id so a card without a title stays usable', () => {
    const cards = toCardSet(recordsPayload);

    expect(cards.items[1].title).toBe('18ef7549-5555-6666-7777-888888888888');
    // Whitespace is not a title either.
    expect(cards.items[2].title).toBe('18ef7549-9999-aaaa-bbbb-cccccccccccc');
  });

  it('returns an empty set instead of throwing on an unexpected payload', () => {
    expect(toCardSet({ title: 'Not Found' })).toEqual({ typeName: '', items: [] });
    expect(toCardSet(null).items).toEqual([]);
  });
});
