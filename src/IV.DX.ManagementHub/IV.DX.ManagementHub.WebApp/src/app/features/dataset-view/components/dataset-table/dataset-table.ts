import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { MatSortModule, type Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';

import type { DatasetColumn, DatasetRow } from '../../models/dataset-table';
import { sortDatasetRows } from '../../sort-rows';
import { UnitActions } from '@shared/units/unit-actions/unit-actions';

/**
 * Name of the trailing actions column.
 *
 * Prefixed so it can never collide with a column coming from DX metadata.
 */
const ACTIONS_COLUMN = '__actions';

/**
 * Table of a dataset view: columns and rows are handed in, the component sorts
 * them, reports the clicked row and hosts the per-row actions.
 */
@Component({
  selector: 'mh-dataset-table',
  imports: [MatSortModule, MatTableModule, UnitActions],
  templateUrl: './dataset-table.html',
  styleUrl: './dataset-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DatasetTable {
  readonly columns = input.required<readonly DatasetColumn[]>();
  readonly rows = input.required<readonly DatasetRow[]>();
  readonly selectedId = model<string | null>(null);

  /** DX unit type of the rows; the actions need it to address a record. */
  readonly typeName = input('');
  readonly canEdit = input(false);
  readonly canDelete = input(false);
  readonly canExport = input(false);

  readonly changed = output<void>();
  readonly deleted = output<string>();
  readonly failed = output<string>();

  protected readonly actionsColumn = ACTIONS_COLUMN;

  protected readonly sort = signal<Sort | null>(null);

  protected readonly hasActions = computed(
    () => this.typeName() !== '' && (this.canEdit() || this.canDelete() || this.canExport()),
  );

  protected readonly columnNames = computed(() => this.columns().map((column) => column.name));

  protected readonly displayedColumns = computed(() =>
    this.hasActions() ? [...this.columnNames(), ACTIONS_COLUMN] : this.columnNames(),
  );

  protected readonly sortedRows = computed(() => {
    const sort = this.sort();

    return sort === null ? this.rows() : sortDatasetRows(this.rows(), sort.active, sort.direction);
  });

  protected select(row: DatasetRow): void {
    this.selectedId.set(this.selectedId() === row.id ? null : row.id);
  }
}
