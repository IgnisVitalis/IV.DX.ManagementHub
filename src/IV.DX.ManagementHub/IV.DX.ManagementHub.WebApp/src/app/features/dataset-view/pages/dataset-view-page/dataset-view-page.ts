import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';

import { DatasetTable } from '../../components/dataset-table/dataset-table';
import type { DatasetRow } from '../../models/dataset-table';
import { errorMessage } from '@core/api/resource';
import { Notice } from '@shared/ui/notice/notice';
import { SplitHandle } from '@shared/ui/split-handle/split-handle';
import { UnitActions } from '@shared/units/unit-actions/unit-actions';
import {
  UnitEditDialog,
  type UnitEditDialogData,
} from '@shared/units/unit-edit-dialog/unit-edit-dialog';
import { UnitPreview } from '../../components/unit-preview/unit-preview';
import { DatasetViewService } from '../../services/dataset-view.service';

/**
 * One metadata-driven table screen.
 *
 * Deliberately headerless: the definition's name is a DX-internal label
 * ("DXUnitActionDefinition DataSetView") that repeats what the navigation entry
 * already says, so the table starts right at the top.
 */
@Component({
  selector: 'mh-dataset-view-page',
  imports: [
    Notice,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    DatasetTable,
    UnitPreview,
    SplitHandle,
    UnitActions,
  ],
  templateUrl: './dataset-view-page.html',
  styleUrl: './dataset-view-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DatasetViewService],
})
export class DatasetViewPage {
  private readonly view = inject(DatasetViewService);
  private readonly dialog = inject(MatDialog);

  protected readonly isLoading = this.view.isLoading;
  protected readonly isUnresolved = this.view.isUnresolved;
  protected readonly error = errorMessage(this.view.error);
  protected readonly table = this.view.table;
  protected readonly definition = this.view.definition;

  protected readonly selectedIds = signal<readonly string[]>([]);

  /** Rows behind the selection, for the summary shown next to bulk actions. */
  protected readonly selectedRows = computed(() => {
    const ids = new Set(this.selectedIds());

    return this.table().rows.filter((row) => ids.has(row.id));
  });

  /** First non-empty cell of a row: the closest thing to a title it has. */
  protected labelOf(row: DatasetRow): string {
    const value = Object.values(row.display).find((text) => text !== '' && text !== '—');

    return value ?? row.id;
  }

  protected readonly singleSelectedId = computed(() =>
    this.selectedIds().length === 1 ? this.selectedIds()[0] : null,
  );

  /** Failure of a row action, shown above the table. */
  protected readonly actionError = signal<string | null>(null);

  protected readonly canEdit = computed(() => this.definition()?.isEditable ?? false);
  protected readonly canDelete = computed(() => this.definition()?.isDeletable ?? false);
  protected readonly canExport = computed(() => this.definition()?.isExportable ?? false);

  /** Rows were deleted: let go of them before the table reloads. */
  protected onDeleted(ids: readonly string[]): void {
    const gone = new Set(ids);

    this.selectedIds.update((selected) => selected.filter((id) => !gone.has(id)));
  }

  protected onActionFailed(message: string): void {
    this.actionError.set(message);
  }

  /** The DX unit type the rows belong to; the preview needs it to load a record. */
  protected readonly typeName = computed(() => this.table().typeName);

  protected readonly isEmpty = computed(
    () => !this.isLoading() && !this.error() && this.table().rows.length === 0,
  );

  protected readonly canCreate = computed(() => this.definition()?.isCreatable ?? false);

  /**
   * Opens the editor with no record behind it. On success the new element is
   * selected, so the preview shows what was just created.
   */
  protected async create(): Promise<void> {
    const data: UnitEditDialogData = { typeName: this.typeName() };
    const ref = this.dialog.open<UnitEditDialog, UnitEditDialogData, string | boolean>(
      UnitEditDialog,
      { data, width: 'min(90vw, 640px)' },
    );

    const createdId = await firstValueFrom(ref.afterClosed());

    if (typeof createdId === 'string') {
      this.view.reload();
      this.selectedIds.set([createdId]);
    }
  }

  /** Width of the preview pane in pixels, adjusted by dragging the divider. */
  protected readonly previewWidth = signal(380);

  protected resizePreview(delta: number): void {
    // Dragging the divider left widens the preview, hence the subtraction.
    this.previewWidth.update((width) => Math.min(900, Math.max(260, width - delta)));
  }

  protected clearSelection(): void {
    this.selectedIds.set([]);
  }

  /** Explicit refresh: start from a clean slate. */
  protected reload(): void {
    this.selectedIds.set([]);
    this.view.reload();
  }

  /**
   * A record was saved: refresh the rows but stay on the element, so the user
   * sees the result of the edit. Deletion closes the preview separately.
   */
  protected reloadRows(): void {
    this.actionError.set(null);
    this.view.reload();
  }
}
