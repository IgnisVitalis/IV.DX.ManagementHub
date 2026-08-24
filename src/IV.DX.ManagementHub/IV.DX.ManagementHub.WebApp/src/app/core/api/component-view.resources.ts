import { computed, inject, type Signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';

import { InstancesService } from '@core/instances/instances.service';
import { resourceValue } from './resource';

export interface ComponentViewResources<TDefinition, TData> {
  readonly definition: Signal<TDefinition | null>;
  readonly data: Signal<TData>;
  readonly isLoading: Signal<boolean>;
  readonly error: Signal<Error | undefined>;
  /** The definition loaded but points at nothing to render. */
  readonly isUnresolved: Signal<boolean>;
  reload(): void;
}

export interface ComponentViewOptions<TDefinition, TData> {
  /** DX unit type of the presentation component, e.g. `DXPDataSetViewUnit`. */
  readonly definitionType: string;
  readonly parseDefinition: (raw: unknown) => TDefinition | null;
  /** Path of the data the definition points at, relative to the instance base. */
  readonly dataPath: (definition: TDefinition) => string;
  readonly parseData: (raw: unknown) => TData;
  readonly emptyData: TData;
}

/**
 * Loads a presentation component from the route: first its definition, then the
 * data that definition points at.
 *
 * Both the dataset and the card screens work this way and differ only in which
 * unit type they load and where the data lives, so the chain — including the
 * loading, error and "unresolved" semantics — is defined once here.
 *
 * Call from an injection context: the component id comes from the route and the
 * API prefix from the current instance.
 */
export function componentViewResources<TDefinition, TData>(
  options: ComponentViewOptions<TDefinition, TData>,
): ComponentViewResources<TDefinition, TData> {
  const instances = inject(InstancesService);
  const params = toSignal(inject(ActivatedRoute).paramMap);

  const componentId = computed(() => params()?.get('componentId') ?? undefined);

  const definitionResource = httpResource(
    () => {
      const base = instances.apiBase();
      const id = componentId();

      return base === undefined || id === undefined
        ? undefined
        : `${base}/${options.definitionType}/${id}`;
    },
    { parse: options.parseDefinition, defaultValue: null },
  );

  const definition = resourceValue(definitionResource, null);

  const dataResource = httpResource(
    () => {
      const base = instances.apiBase();
      const loaded = definition();

      return base === undefined || loaded === null
        ? undefined
        : `${base}/${options.dataPath(loaded)}`;
    },
    { parse: options.parseData, defaultValue: options.emptyData },
  );

  return {
    definition,
    data: resourceValue(dataResource, options.emptyData),
    isLoading: computed(() => definitionResource.isLoading() || dataResource.isLoading()),
    error: computed(() => definitionResource.error() ?? dataResource.error()),
    isUnresolved: computed(
      () => definitionResource.status() === 'resolved' && definition() === null,
    ),
    reload: () => {
      definitionResource.reload();
      dataResource.reload();
    },
  };
}
