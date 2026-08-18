import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import {
  applyCollectionEdits,
  collectionElements,
  editableColumns,
  newCollectionRow,
  rowLabel,
  toCollectionEdits,
  visibleRows,
  type CollectionEdits,
  type CollectionRow,
} from '@core/units/collection-edit';
import { describeError } from '@core/api/describe-error';
import { toEditorFields, type EditorField } from '@core/units/editor-field';
import type { UnitElement } from '@core/units/models/unit-structure';
import { UnitCommands } from '@core/units/unit-commands.service';
import {
  applyEdits,
  buildNewRecord,
  toEditValues,
  toNewEditValues,
  type EditValue,
} from '@core/units/unit-record.patch';
import { unitRecordResources } from '@core/units/unit-record.resources';
import { UnitField } from '../unit-field/unit-field';

export interface UnitEditDialogData {
  readonly typeName: string;
  /** Omitted when creating: there is no record to load or patch. */
  readonly id?: string;
}

/** Which collection row is open in the row form. */
interface EditingRow {
  readonly element: string;
  readonly rowId: string;
}

/** Creates or edits one DX unit: its main element and its collections. */
@Component({
  selector: 'mh-unit-edit-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    MatProgressBarModule,
    UnitField,
  ],
  templateUrl: './unit-edit-dialog.html',
  styleUrl: './unit-edit-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitEditDialog {
  /** Closes with the new record's id when creating, or `true` after an edit. */
  private readonly dialogRef = inject(MatDialogRef<UnitEditDialog, string | boolean>);
  private readonly commands = inject(UnitCommands);

  protected readonly data = inject<UnitEditDialogData>(MAT_DIALOG_DATA);
  protected readonly isNew = this.data.id === undefined;

  private readonly typeName = signal(this.data.typeName);
  private readonly id = signal(this.data.id);
  private readonly resources = unitRecordResources(this.typeName, this.id);

  protected readonly isLoading = this.resources.isLoading;
  protected readonly loadError = computed(() => {
    const error = this.resources.error();
    return error === undefined ? null : describeError(error);
  });

  protected readonly fields = computed(() => toEditorFields(this.resources.structure()));
  protected readonly elements = computed(() => collectionElements(this.resources.structure()));

  /**
   * State resets whenever the record reloads, which is exactly what
   * `linkedSignal` is for — no effect syncing state behind the scenes.
   */
  protected readonly values = linkedSignal<
    { fields: readonly EditorField[]; record: unknown },
    Record<string, EditValue>
  >({
    source: () => ({ fields: this.fields(), record: this.resources.record() }),
    computation: ({ fields, record }) => {
      const columns = fields.map((field) => field.column);
      return this.isNew ? toNewEditValues(columns) : toEditValues(columns, record);
    },
  });

  protected readonly collections = linkedSignal<
    { elements: readonly UnitElement[]; record: unknown },
    CollectionEdits
  >({
    source: () => ({ elements: this.elements(), record: this.resources.record() }),
    computation: ({ record }) => toCollectionEdits(this.resources.structure(), record),
  });

  protected readonly editing = signal<EditingRow | null>(null);

  protected readonly saveError = signal<string | null>(null);
  protected readonly isSaving = signal(false);

  /** Required main-element fields the user has left empty. */
  protected readonly missing = computed(() => {
    const values = this.values();

    return new Set(
      this.fields()
        .filter((field) => field.required)
        .filter((field) => {
          const value = values[field.column.name];
          return value === null || value === '';
        })
        .map((field) => field.column.name),
    );
  });

  protected readonly canSave = computed(
    () =>
      !this.isLoading() && !this.isSaving() && this.missing().size === 0 && this.editing() === null,
  );

  protected valueOf(field: EditorField): EditValue {
    return this.values()[field.column.name] ?? null;
  }

  protected set(field: EditorField, value: EditValue): void {
    this.values.update((values) => ({ ...values, [field.column.name]: value }));
  }

  // --- collections ---

  private rowsOf(element: UnitElement): readonly CollectionRow[] {
    return this.collections()[element.name] ?? [];
  }

  /** What the list shows: DX's own column definitions stay hidden. */
  protected visibleRowsOf(element: UnitElement): readonly CollectionRow[] {
    return visibleRows(element, this.rowsOf(element));
  }

  protected labelOf(element: UnitElement, row: CollectionRow): string {
    return rowLabel(element, row);
  }

  protected fieldsOf(element: UnitElement): readonly EditorField[] {
    return toEditorFields({
      name: element.name,
      main: { name: element.name, columns: editableColumns(element) },
      requiredSingle: [],
      optionalSingle: [],
      requiredMulti: [],
      optionalMulti: [],
    });
  }

  protected editingRow(element: UnitElement): CollectionRow | null {
    const editing = this.editing();

    if (editing === null || editing.element !== element.name) {
      return null;
    }

    return this.rowsOf(element).find((row) => row.id === editing.rowId) ?? null;
  }

  protected addRow(element: UnitElement): void {
    const row = newCollectionRow(element);

    this.collections.update((edits) => ({
      ...edits,
      [element.name]: [...(edits[element.name] ?? []), row],
    }));

    this.editing.set({ element: element.name, rowId: row.id });
  }

  protected removeRow(element: UnitElement, row: CollectionRow): void {
    this.collections.update((edits) => ({
      ...edits,
      [element.name]: (edits[element.name] ?? []).filter((existing) => existing.id !== row.id),
    }));
  }

  protected openRow(element: UnitElement, row: CollectionRow): void {
    this.editing.set({ element: element.name, rowId: row.id });
  }

  protected closeRow(): void {
    this.editing.set(null);
  }

  protected setRowValue(
    element: UnitElement,
    row: CollectionRow,
    field: EditorField,
    value: EditValue,
  ): void {
    this.collections.update((edits) => ({
      ...edits,
      [element.name]: (edits[element.name] ?? []).map((existing) =>
        existing.id === row.id
          ? { ...existing, values: { ...existing.values, [field.column.name]: value } }
          : existing,
      ),
    }));
  }

  protected rowValueOf(row: CollectionRow, field: EditorField): EditValue {
    return row.values[field.column.name] ?? null;
  }

  // --- saving ---

  protected async save(): Promise<void> {
    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      const structure = this.resources.structure();
      const columns = this.fields().map((field) => field.column);

      if (this.data.id === undefined) {
        const base = buildNewRecord(this.data.typeName, columns, this.values());
        const payload = applyCollectionEdits(base, structure, this.collections());
        const createdId = await this.commands.create(this.data.typeName, payload);
        this.dialogRef.close(createdId);
      } else {
        const patched = applyEdits(this.resources.record(), columns, this.values());
        const payload = applyCollectionEdits(patched, structure, this.collections());
        await this.commands.update(this.data.typeName, this.data.id, payload);
        this.dialogRef.close(true);
      }
    } catch (error) {
      this.saveError.set(describeError(error));
    } finally {
      this.isSaving.set(false);
    }
  }
}
