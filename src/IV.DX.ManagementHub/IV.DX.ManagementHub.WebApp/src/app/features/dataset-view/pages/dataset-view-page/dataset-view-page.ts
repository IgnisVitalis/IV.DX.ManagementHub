import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';

import { DatasetTable } from '../../components/dataset-table/dataset-table';
import { describeError } from '@core/api/describe-error';
import { SplitHandle } from '@shared/ui/split-handle/split-handle';
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
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    DatasetTable,
    UnitPreview,
    SplitHandle,
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
  protected readonly error = computed(() => {
    const error = this.view.error();
    return error === undefined ? null : describeError(error);
  });
  protected readonly table = this.view.table;
  protected readonly definition = this.view.definition;

  protected readonly selectedId = signal<string | null>(null);

  /** Failure of a row action, shown above the table. */
  protected readonly actionError = signal<string | null>(null);

  protected readonly canEdit = computed(() => this.definition()?.isEditable ?? false);
  protected readonly canDelete = computed(() => this.definition()?.isDeletable ?? false);
  protected readonly canExport = computed(() => this.definition()?.isExportable ?? false);

  /** A row was deleted: let go of it before the rows reload. */
  protected onDeleted(id: string): void {
    if (this.selectedId() === id) {
      this.selectedId.set(null);
    }
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
      this.selectedId.set(createdId);
    }
  }

  /** Width of the preview pane in pixels, adjusted by dragging the divider. */
  protected readonly previewWidth = signal(380);

  protected resizePreview(delta: number): void {
    // Dragging the divider left widens the preview, hence the subtraction.
    this.previewWidth.update((width) => Math.min(900, Math.max(260, width - delta)));
  }

  protected clearSelection(): void {
    this.selectedId.set(null);
  }

  /** Explicit refresh: start from a clean slate. */
  protected reload(): void {
    this.selectedId.set(null);
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
