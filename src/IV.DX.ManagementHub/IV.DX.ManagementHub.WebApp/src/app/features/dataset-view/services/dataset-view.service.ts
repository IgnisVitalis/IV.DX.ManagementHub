import { Injectable } from '@angular/core';

import { componentViewResources } from '@core/api/component-view.resources';
import { toDatasetTable, toDatasetViewDefinition } from '../dataset-view.mapper';
import { EMPTY_DATASET_TABLE } from '../models/dataset-table';

/**
 * One dataset view: its definition, then the rows of the query it points at.
 *
 * Provided by the page component rather than in root — it is scoped to the
 * screen it belongs to.
 */
@Injectable()
export class DatasetViewService {
  private readonly view = componentViewResources({
    definitionType: 'DXPDataSetViewUnit',
    parseDefinition: toDatasetViewDefinition,
    dataPath: (definition) => `DXQueryResult/${definition.queryId}`,
    parseData: toDatasetTable,
    emptyData: EMPTY_DATASET_TABLE,
  });

  readonly definition = this.view.definition;
  readonly table = this.view.data;
  readonly isLoading = this.view.isLoading;
  readonly error = this.view.error;
  readonly isUnresolved = this.view.isUnresolved;

  reload(): void {
    this.view.reload();
  }
}
