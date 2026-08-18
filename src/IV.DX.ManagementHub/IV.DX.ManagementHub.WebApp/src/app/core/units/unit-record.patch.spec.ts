import { DXColumnType } from './dx-column-type';
import type { UnitColumn } from './models/unit-structure';
import {
  applyEdits,
  buildNewRecord,
  toEditValue,
  toEditValues,
  toNewEditValues,
  toWireValue,
} from './unit-record.patch';

function column(name: string, type: number, extra: Partial<UnitColumn> = {}): UnitColumn {
  return {
    name,
    type,
    enumValues: null,
    relationValues: null,
    allowNull: true,
    length: null,
    defaultValue: null,
    ...extra,
  };
}

const name = column('Name', DXColumnType.String);
const order = column('Order', DXColumnType.Int);
const flag = column('Read', DXColumnType.Bool);
const secret = column('SecretHash', DXColumnType.HashedString);

/** Same shape the API returns, with a collection that must survive a save. */
const record = {
  Meta: { Kind: 'DXUnit', Type: 'DXRoleUnit', Op: 'Sync' },
  Data: {
    Items: [
      {
        DXElements: { DXUnitGrantElement: { Data: { Items: [{ Id: 'g1', Read: true }] } } },
        Id: 'r1',
        Name: 'MH Instance Manager',
        Order: 10,
        Read: false,
        SecretHash: '',
      },
    ],
  },
};

describe('toEditValue', () => {
  it('starts a secret field empty even when the API sent something', () => {
    expect(toEditValue(secret, 'hash')).toBe('');
  });

  it('keeps numbers numeric and blanks null', () => {
    expect(toEditValue(order, 10)).toBe(10);
    expect(toEditValue(order, null)).toBeNull();
    expect(toEditValue(name, null)).toBe('');
  });

  it('trims a timestamp down to what datetime-local accepts', () => {
    expect(toEditValue(column('When', DXColumnType.DateTime), '2026-08-11T16:12:19.713468Z')).toBe(
      '2026-08-11T16:12',
    );
  });
});

describe('toWireValue', () => {
  it('sends null for an emptied field', () => {
    expect(toWireValue(name, '')).toBeNull();
    expect(toWireValue(order, null)).toBeNull();
  });

  it('sends booleans, not strings', () => {
    expect(toWireValue(flag, true)).toBe(true);
    expect(toWireValue(flag, null)).toBe(false);
  });

  it('parses a numeric field back to a number', () => {
    expect(toWireValue(order, '42')).toBe(42);
    expect(toWireValue(order, 'nonsense')).toBeNull();
  });
});

describe('toEditValues', () => {
  it('reads the starting values out of the record', () => {
    expect(toEditValues([name, order, flag, secret], record)).toEqual({
      Name: 'MH Instance Manager',
      Order: 10,
      Read: false,
      SecretHash: '',
    });
  });
});

describe('applyEdits', () => {
  const columns = [name, order, flag, secret];

  it('writes the edited values without touching the collections', () => {
    const patched = applyEdits(record, columns, {
      Name: 'Renamed',
      Order: 20,
      Read: true,
      SecretHash: '',
    }) as typeof record;

    const item = patched.Data.Items[0];

    expect(item.Name).toBe('Renamed');
    expect(item.Order).toBe(20);
    expect(item.Read).toBe(true);
    expect(item.DXElements).toEqual(record.Data.Items[0].DXElements);
    expect(patched.Meta).toEqual(record.Meta);
  });

  it('does not mutate the loaded record', () => {
    const before = JSON.stringify(record);
    applyEdits(record, columns, { Name: 'Renamed' });
    expect(JSON.stringify(record)).toBe(before);
  });

  it('leaves an untouched secret out of the payload', () => {
    const patched = applyEdits(record, columns, { SecretHash: '' }) as typeof record;

    // The API redacts secrets on read; writing the empty string back would wipe it.
    expect(patched.Data.Items[0].SecretHash).toBe('');
  });

  it('sends a secret the user actually typed', () => {
    const patched = applyEdits(record, columns, { SecretHash: 'new-secret' }) as typeof record;

    expect(patched.Data.Items[0].SecretHash).toBe('new-secret');
  });

  it('refuses a payload with no record in it', () => {
    expect(() => applyEdits({ Data: { Items: [] } }, columns, {})).toThrow();
  });
});

describe('toNewEditValues', () => {
  it('reads DX boolean defaults, which are written as text', () => {
    // '0' and 'false' are truthy strings; coercing them would flip the meaning.
    const off = column('IsCreatable', DXColumnType.Bool, { defaultValue: '0' });
    const alsoOff = column('IsPublicRead', DXColumnType.Bool, { defaultValue: 'false' });
    const on = column('IsEditable', DXColumnType.Bool, { defaultValue: 'true' });

    expect(toNewEditValues([off, alsoOff, on])).toEqual({
      IsCreatable: false,
      IsPublicRead: false,
      IsEditable: true,
    });
  });

  it('falls back to an empty value when no default is declared', () => {
    expect(toNewEditValues([name, order, flag])).toEqual({
      Name: '',
      Order: null,
      Read: false,
    });
  });

  it('parses a numeric default', () => {
    expect(toNewEditValues([column('Order', DXColumnType.Int, { defaultValue: '10' })])).toEqual({
      Order: 10,
    });
  });
});

describe('buildNewRecord', () => {
  it('wraps the values in the envelope the API expects, without an id', () => {
    const payload = buildNewRecord('DXRoleUnit', [name, order, flag], {
      Name: 'New role',
      Order: 5,
      Read: true,
    }) as { Meta: unknown; Data: { Items: Record<string, unknown>[] } };

    expect(payload.Meta).toEqual({ Kind: 'DXUnit', Type: 'DXRoleUnit', IsMulti: true });
    expect(payload.Data.Items).toEqual([{ Name: 'New role', Order: 5, Read: true }]);
    expect(payload.Data.Items[0]['Id']).toBeUndefined();
  });

  it('omits a secret the user left empty', () => {
    const payload = buildNewRecord('DXIdentityLoginUnit', [name, secret], {
      Name: 'admin',
      SecretHash: '',
    }) as { Data: { Items: Record<string, unknown>[] } };

    expect(payload.Data.Items[0]).toEqual({ Name: 'admin' });
  });
});
