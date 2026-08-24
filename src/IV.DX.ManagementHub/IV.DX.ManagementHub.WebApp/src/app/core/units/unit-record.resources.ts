import { computed, inject, type Signal } from '@angular/core';
import { httpResource } from '@angular/common/http';

import { resourceValue } from '@core/api/resource';
import { InstancesService } from '@core/instances/instances.service';
import type { UnitStructure } from './models/unit-structure';
import { toUnitStructure } from './unit-structure.mapper';

export interface UnitRecordResources {
  readonly structure: Signal<UnitStructure | null>;
  /** Raw payload as the API returned it, kept intact so an edit can patch it. */
  readonly record: Signal<unknown>;
  readonly isLoading: Signal<boolean>;
  readonly error: Signal<Error | undefined>;
  reload(): void;
}

/**
 * Loads the structure of a DX unit type and one record of it. Both the preview
 * and the editor build on this, so the two never disagree about what was loaded.
 *
 * A factory rather than an injectable class: the inputs are signals owned by the
 * calling component, and a DI-created service cannot receive those. Call it from
 * an injection context (a component field initializer). HTTP still stays out of
 * the component itself.

 */
export function unitRecordResources(
  typeName: Signal<string | undefined>,
  id: Signal<string | undefined>,
): UnitRecordResources {
  const instances = inject(InstancesService);

  const structureResource = httpResource(
    () => {
      const base = instances.apiBase();
      const type = typeName();

      return base === undefined || type === undefined
        ? undefined
        : `${base}/DXUnitStructure/${type}`;
    },
    { parse: toUnitStructure, defaultValue: null },
  );

  const recordResource = httpResource(
    () => {
      const base = instances.apiBase();
      const type = typeName();
      const recordId = id();

      return base === undefined || type === undefined || recordId === undefined
        ? undefined
        : `${base}/${type}/${recordId}`;
    },
    { defaultValue: null },
  );

  return {
    structure: resourceValue(structureResource, null),
    record: resourceValue(recordResource, null),
    isLoading: computed(() => structureResource.isLoading() || recordResource.isLoading()),
    error: computed(() => structureResource.error() ?? recordResource.error()),
    reload: () => {
      structureResource.reload();
      recordResource.reload();
    },
  };
}
