import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
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

/** Name of the leading selection column, prefixed for the same reason. */
const SELECT_COLUMN = '__select';

/**
 * Table of a dataset view: columns and rows are handed in, the component sorts
 * them, reports the clicked row and hosts the per-row actions.
 */
@Component({
  selector: 'mh-dataset-table',
  imports: [MatCheckboxModule, MatSortModule, MatTableModule, UnitActions],
  templateUrl: './dataset-table.html',
  styleUrl: './dataset-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DatasetTable {
  readonly columns = input.required<readonly DatasetColumn[]>();
  readonly rows = input.required<readonly DatasetRow[]>();
  /** Ids of the selected rows, in selection order. */
  readonly selectedIds = model<readonly string[]>([]);

  /** DX unit type of the rows; the actions need it to address a record. */
  readonly typeName = input('');
  readonly canEdit = input(false);
  readonly canDelete = input(false);
  readonly canExport = input(false);

  readonly changed = output<void>();
  readonly deleted = output<readonly string[]>();
  readonly failed = output<string>();

  protected readonly actionsColumn = ACTIONS_COLUMN;
  protected readonly selectColumn = SELECT_COLUMN;

  protected readonly sort = signal<Sort | null>(null);

  protected readonly hasActions = computed(
    () => this.typeName() !== '' && (this.canEdit() || this.canDelete() || this.canExport()),
  );

  protected readonly columnNames = computed(() => this.columns().map((column) => column.name));

  protected readonly displayedColumns = computed(() => {
    const columns = [SELECT_COLUMN, ...this.columnNames()];

    return this.hasActions() ? [...columns, ACTIONS_COLUMN] : columns;
  });

  private readonly selection = computed(() => new Set(this.selectedIds()));

  protected isSelected(row: DatasetRow): boolean {
    return this.selection().has(row.id);
  }

  protected readonly allSelected = computed(() => {
    const rows = this.rows();

    return rows.length > 0 && rows.every((row) => this.selection().has(row.id));
  });

  /** Some but not all — drives the checkbox's indeterminate state. */
  protected readonly someSelected = computed(
    () => this.selectedIds().length > 0 && !this.allSelected(),
  );

  protected toggleAll(): void {
    this.selectedIds.set(this.allSelected() ? [] : this.rows().map((row) => row.id));
  }

  protected readonly sortedRows = computed(() => {
    const sort = this.sort();

    return sort === null ? this.rows() : sortDatasetRows(this.rows(), sort.active, sort.direction);
  });

  /** Clicking a row toggles it, the same as its checkbox. */
  protected select(row: DatasetRow): void {
    this.selectedIds.update((ids) =>
      ids.includes(row.id) ? ids.filter((id) => id !== row.id) : [...ids, row.id],
    );
  }
}
