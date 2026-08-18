import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { httpResource } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

import { InstancesService } from '@core/instances/instances.service';
import { EMPTY_DATASET_TABLE } from '../models/dataset-table';
import { toDatasetTable, toDatasetViewDefinition } from '../dataset-view.mapper';

/**
 * Loads one dataset view: first its definition, then the rows produced by the
 * query the definition points at.
 *
 * Provided by the page component, not in root — the store is scoped to the
 * screen it belongs to, and reading the route parameter here keeps the page
 * component free of plumbing.
 *
 * Nothing outside this class touches `resource.value()` directly. That signal
 * throws a `ResourceValueError` while the resource is in an error state — a
 * failed request would otherwise take down both the template that renders the
 * error and the dependent resource below, leaving the screen stuck on its
 * spinner. `hasValue()` is the guard that never throws.
 */
@Injectable()
export class DatasetViewService {
  private readonly instances = inject(InstancesService);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);

  readonly componentId = computed(() => this.params()?.get('componentId') ?? undefined);

  private readonly definitionResource = httpResource(
    () => {
      const base = this.instances.apiBase();
      const id = this.componentId();

      return base === undefined || id === undefined
        ? undefined
        : `${base}/DXPDataSetViewUnit/${id}`;
    },
    { parse: toDatasetViewDefinition, defaultValue: null },
  );

  readonly definition = computed(() =>
    this.definitionResource.hasValue() ? this.definitionResource.value() : null,
  );

  /** Waits for the definition: without a query id there is no request to make. */
  private readonly tableResource = httpResource(
    () => {
      const base = this.instances.apiBase();
      const queryId = this.definition()?.queryId;

      return base === undefined || queryId === undefined
        ? undefined
        : `${base}/DXQueryResult/${queryId}`;
    },
    { parse: toDatasetTable, defaultValue: EMPTY_DATASET_TABLE },
  );

  readonly table = computed(() =>
    this.tableResource.hasValue() ? this.tableResource.value() : EMPTY_DATASET_TABLE,
  );

  readonly isLoading = computed(
    () => this.definitionResource.isLoading() || this.tableResource.isLoading(),
  );

  readonly error = computed(() => this.definitionResource.error() ?? this.tableResource.error());

  /** The definition loaded but carried no query — nothing can be rendered. */
  readonly isUnresolved = computed(
    () => this.definitionResource.status() === 'resolved' && this.definition() === null,
  );

  reload(): void {
    this.definitionResource.reload();
    this.tableResource.reload();
  }
}
