import { dataBlockItems } from '@core/api/models/dx-data-block';
import type { Instance } from './models/instance';

interface InstanceRow {
  readonly Id: string;
  readonly Key: string | null;
  readonly DXTitle: string | null;
}

/**
 * Maps the `MHInstanceUnit` records onto instances.
 *
 * Records without a key are dropped: the key is what addresses an instance, so
 * one without it cannot be selected or requested.
 */
export function toInstances(raw: unknown): readonly Instance[] {
  return dataBlockItems<InstanceRow>(raw)
    .filter((row) => (row.Key ?? '').trim() !== '')
    .map((row) => ({
      id: row.Id,
      key: row.Key!.trim(),
      title: row.DXTitle?.trim() ? row.DXTitle.trim() : row.Key!.trim(),
    }))
    .sort((a, b) => a.title.localeCompare(b.title));
}
