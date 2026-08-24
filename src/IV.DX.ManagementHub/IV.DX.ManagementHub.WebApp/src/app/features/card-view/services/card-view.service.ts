import { Injectable } from '@angular/core';

import { componentViewResources } from '@core/api/component-view.resources';
import { toCardSet, toCardViewDefinition } from '../card-view.mapper';
import { EMPTY_CARD_SET } from '../models/card-view';

/**
 * One card view: its definition, then the records of the unit definition it
 * points at — addressed by definition id, not by type name.
 */
@Injectable()
export class CardViewService {
  private readonly view = componentViewResources({
    definitionType: 'DXPCardViewUnit',
    parseDefinition: toCardViewDefinition,
    dataPath: (definition) => definition.unitDefinitionId,
    parseData: toCardSet,
    emptyData: EMPTY_CARD_SET,
  });

  readonly definition = this.view.definition;
  readonly cards = this.view.data;
  readonly isLoading = this.view.isLoading;
  readonly error = this.view.error;
  readonly isUnresolved = this.view.isUnresolved;

  reload(): void {
    this.view.reload();
  }
}
