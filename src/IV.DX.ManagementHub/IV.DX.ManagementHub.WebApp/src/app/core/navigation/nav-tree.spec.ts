import type { NavItem } from './models/nav-item';
import { buildNavForest, flattenNavTree } from './nav-tree';

function item(partial: Partial<NavItem> & Pick<NavItem, 'id'>): NavItem {
  return {
    name: partial.id,
    parentId: null,
    order: 0,
    componentType: null,
    componentId: null,
    ...partial,
  };
}

const names = (nodes: readonly { item: NavItem }[]) => nodes.map((n) => n.item.name);

describe('buildNavForest', () => {
  it('nests children under their parent', () => {
    const forest = buildNavForest([item({ id: 'child', parentId: 'root' }), item({ id: 'root' })]);

    expect(names(forest)).toEqual(['root']);
    expect(names(forest[0].children)).toEqual(['child']);
  });

  it('orders siblings by order, then by id', () => {
    const forest = buildNavForest([
      item({ id: 'b', order: 20 }),
      item({ id: 'c', order: 10 }),
      item({ id: 'a', order: 10 }),
    ]);

    expect(names(forest)).toEqual(['a', 'c', 'b']);
  });

  it('treats an entry with a missing parent as a root', () => {
    const forest = buildNavForest([item({ id: 'orphan', parentId: 'gone' })]);

    expect(names(forest)).toEqual(['orphan']);
  });

  it('keeps the first of two entries sharing an id', () => {
    const forest = buildNavForest([
      item({ id: 'dup', name: 'first' }),
      item({ id: 'dup', name: 'second' }),
    ]);

    expect(names(forest)).toEqual(['first']);
  });

  it('returns an empty forest for no items', () => {
    expect(buildNavForest([])).toEqual([]);
  });
});

describe('flattenNavTree', () => {
  const forest = buildNavForest([
    item({ id: 'root', order: 10 }),
    item({ id: 'branch', parentId: 'root', order: 10 }),
    item({ id: 'leaf', parentId: 'branch', order: 10, componentId: 'c1' }),
  ]);

  const link = (i: NavItem) => (i.componentId === null ? null : `/view/${i.componentId}`);

  it('shows only roots while nothing is expanded', () => {
    const rows = flattenNavTree(forest, new Set(), link);

    expect(rows.map((r) => r.name)).toEqual(['root']);
    expect(rows[0]).toMatchObject({ level: 0, hasChildren: true, expanded: false, link: null });
  });

  it('reveals a level per expanded ancestor', () => {
    expect(flattenNavTree(forest, new Set(['root']), link).map((r) => r.name)) //
      .toEqual(['root', 'branch']);

    const rows = flattenNavTree(forest, new Set(['root', 'branch']), link);
    expect(rows.map((r) => r.name)).toEqual(['root', 'branch', 'leaf']);
    expect(rows.map((r) => r.level)).toEqual([0, 1, 2]);
  });

  it('keeps a descendant hidden when its parent stays collapsed', () => {
    expect(flattenNavTree(forest, new Set(['branch']), link).map((r) => r.name)).toEqual(['root']);
  });

  it('builds a link only for entries with a component', () => {
    const rows = flattenNavTree(forest, new Set(['root', 'branch']), link);

    expect(rows.at(-1)).toMatchObject({ name: 'leaf', hasChildren: false, link: '/view/c1' });
  });
});
