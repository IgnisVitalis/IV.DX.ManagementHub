import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { httpResource } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

import { InstancesService } from '@core/instances/instances.service';
import { toCardSet, toCardViewDefinition } from '../card-view.mapper';
import { EMPTY_CARD_SET } from '../models/card-view';

/**
 * Loads one card view: its definition, then the records of the unit definition
 * it points at.
 *
 * Same shape as `DatasetViewService`, including the `hasValue()` guards — a
 * resource's `value()` throws while it is in an error state.
 */
@Injectable()
export class CardViewService {
  private readonly instances = inject(InstancesService);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap);

  readonly componentId = computed(() => this.params()?.get('componentId') ?? undefined);

  private readonly definitionResource = httpResource(
    () => {
      const base = this.instances.apiBase();
      const id = this.componentId();

      return base === undefined || id === undefined ? undefined : `${base}/DXPCardViewUnit/${id}`;
    },
    { parse: toCardViewDefinition, defaultValue: null },
  );

  readonly definition = computed(() =>
    this.definitionResource.hasValue() ? this.definitionResource.value() : null,
  );

  /** Records are addressed by the unit definition id, not by a type name. */
  private readonly cardsResource = httpResource(
    () => {
      const base = this.instances.apiBase();
      const definitionId = this.definition()?.unitDefinitionId;

      return base === undefined || definitionId === undefined
        ? undefined
        : `${base}/${definitionId}`;
    },
    { parse: toCardSet, defaultValue: EMPTY_CARD_SET },
  );

  readonly cards = computed(() =>
    this.cardsResource.hasValue() ? this.cardsResource.value() : EMPTY_CARD_SET,
  );

  readonly isLoading = computed(
    () => this.definitionResource.isLoading() || this.cardsResource.isLoading(),
  );

  readonly error = computed(() => this.definitionResource.error() ?? this.cardsResource.error());

  /** The definition loaded but named no unit definition. */
  readonly isUnresolved = computed(
    () => this.definitionResource.status() === 'resolved' && this.definition() === null,
  );

  reload(): void {
    this.definitionResource.reload();
    this.cardsResource.reload();
  }
}
