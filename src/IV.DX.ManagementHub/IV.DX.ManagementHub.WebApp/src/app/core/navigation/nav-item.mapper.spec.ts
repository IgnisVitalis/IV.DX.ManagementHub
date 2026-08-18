import { toNavItems } from './nav-item.mapper';
import { buildNavForest, flattenNavTree } from './nav-tree';
import { navLinkOf } from './nav-link';

/** Links are instance-scoped; the key comes from the route in the real app. */
const linkOf = (item: Parameters<typeof navLinkOf>[0]) => navLinkOf(item, 'Own');

/**
 * Trimmed but otherwise untouched response of
 * `GET /api/management/query-result/{navItemsQueryId}` — three roots is what the
 * seeded ManagementHub metadata actually returns.
 */
const apiResponse = {
  QueryDefinition: [
    { Name: 'Id', Expression: 'Id', Order: -1 },
    { Name: 'Name', Expression: 'Name', Order: 20 },
  ],
  Content: {
    Meta: { Kind: 'DXUnit', Type: 'DXPNavigationItemUnit', IsMulti: true },
    Data: {
      Items: [
        {
          DXElements: null,
          Id: '018fa54b-ca06-73a1-9170-1fc2539dce12',
          TimeStamp: '2026-08-11T16:12:19.412643Z',
          DXTitle: null,
          ParentName: null,
          Name: 'Data definitions',
          Order: 20,
          ParentId: null,
          ComponentId: null,
          ComponentType: null,
        },
        {
          DXElements: null,
          Id: '018fa54b-d1d6-7dbb-85ef-68100e4c8b03',
          TimeStamp: '2026-08-11T16:12:19.420028Z',
          DXTitle: null,
          ParentName: 'Data definitions',
          Name: 'Enums',
          Order: 10,
          ParentId: '018fa54b-ca06-73a1-9170-1fc2539dce12',
          ComponentId: '018fa54b-e000-7000-8000-000000000001',
          ComponentType: '018fa54a-f716-7cff-816f-ca24e1f18cca',
        },
      ],
    },
  },
};

describe('toNavItems', () => {
  it('maps the DX columns onto the domain model', () => {
    expect(toNavItems(apiResponse)).toEqual([
      {
        id: '018fa54b-ca06-73a1-9170-1fc2539dce12',
        name: 'Data definitions',
        parentId: null,
        order: 20,
        componentId: null,
        componentType: null,
      },
      {
        id: '018fa54b-d1d6-7dbb-85ef-68100e4c8b03',
        name: 'Enums',
        parentId: '018fa54b-ca06-73a1-9170-1fc2539dce12',
        order: 10,
        componentId: '018fa54b-e000-7000-8000-000000000001',
        componentType: '018fa54a-f716-7cff-816f-ca24e1f18cca',
      },
    ]);
  });

  it('yields no items instead of throwing on an unexpected payload', () => {
    expect(toNavItems(null)).toEqual([]);
    expect(toNavItems({ error: 'unauthorized' })).toEqual([]);
    expect(toNavItems({ Content: { Data: {} } })).toEqual([]);
  });

  it('feeds the tree and the rendered rows end to end', () => {
    const forest = buildNavForest(toNavItems(apiResponse));
    const rootId = '018fa54b-ca06-73a1-9170-1fc2539dce12';

    expect(flattenNavTree(forest, new Set(), linkOf)).toEqual([
      {
        id: rootId,
        name: 'Data definitions',
        level: 0,
        hasChildren: true,
        expanded: false,
        link: null,
      },
    ]);

    const expanded = flattenNavTree(forest, new Set([rootId]), linkOf);
    expect(expanded.at(-1)).toMatchObject({
      name: 'Enums',
      level: 1,
      hasChildren: false,
      link: '/app/Own/view/018fa54b-e000-7000-8000-000000000001',
    });
  });
});
