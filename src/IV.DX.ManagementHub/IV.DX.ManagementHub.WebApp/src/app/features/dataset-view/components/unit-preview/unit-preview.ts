import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';

import { describeError } from '@core/api/describe-error';
import { toUnitPreview } from '@core/units/unit-preview.mapper';
import { unitRecordResources } from '@core/units/unit-record.resources';
import { UnitActions } from '@shared/units/unit-actions/unit-actions';

/** Details of the row selected in the table, built from DX metadata. */
@Component({
  selector: 'mh-unit-preview',
  imports: [
    MatButtonModule,
    MatExpansionModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    UnitActions,
  ],
  templateUrl: './unit-preview.html',
  styleUrl: './unit-preview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitPreview {
  readonly typeName = input.required<string>();
  readonly id = input.required<string>();

  /** Permissions come from the dataset view definition. */
  readonly canEdit = input(false);
  readonly canDelete = input(false);
  readonly canExport = input(false);

  readonly closed = output<void>();
  /** The record was saved or deleted; the table needs reloading. */
  readonly changed = output<void>();

  // Declared after the inputs on purpose: class fields initialize in order.
  private readonly resources = unitRecordResources(this.typeName, this.id);

  protected readonly preview = computed(() =>
    toUnitPreview(this.resources.structure(), this.resources.record()),
  );

  protected readonly isLoading = this.resources.isLoading;

  protected readonly error = computed(() => {
    const error = this.resources.error();
    return error === undefined ? null : describeError(error);
  });

  protected readonly actionError = signal<string | null>(null);

  protected reload(): void {
    this.resources.reload();
  }

  /** A save happened here: refresh what the panel shows, then tell the page. */
  protected onChanged(): void {
    this.actionError.set(null);
    this.resources.reload();
    this.changed.emit();
  }

  protected onDeleted(): void {
    this.closed.emit();
  }

  protected onFailed(message: string): void {
    this.actionError.set(message);
  }
}
