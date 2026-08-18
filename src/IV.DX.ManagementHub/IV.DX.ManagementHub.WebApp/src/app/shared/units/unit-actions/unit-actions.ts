import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import type { ComponentType } from '@angular/cdk/portal';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { firstValueFrom } from 'rxjs';

import { describeError } from '@core/api/describe-error';
import { UnitCommands } from '@core/units/unit-commands.service';
import { ConfirmDialog } from '@shared/ui/confirm-dialog/confirm-dialog';
import { UnitEditDialog } from '../unit-edit-dialog/unit-edit-dialog';

/**
 * Edit / export / delete for one record.
 *
 * Shared by the table row and the preview header so both offer exactly the same
 * actions; only the labels differ, and rows show icons alone to stay compact.
 */
@Component({
  selector: 'mh-unit-actions',
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './unit-actions.html',
  styleUrl: './unit-actions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitActions {
  private readonly dialog = inject(MatDialog);
  private readonly commands = inject(UnitCommands);

  readonly typeName = input.required<string>();
  readonly id = input.required<string>();

  readonly canEdit = input(false);
  readonly canDelete = input(false);
  readonly canExport = input(false);

  /** The record was saved; rows need reloading. */
  readonly changed = output<void>();
  /** The record is gone; whoever shows it has to let go of the id. */
  readonly deleted = output<string>();
  readonly failed = output<string>();

  protected readonly isRunning = signal(false);

  protected readonly hasAny = computed(
    () => this.canEdit() || this.canDelete() || this.canExport(),
  );

  protected async edit(): Promise<void> {
    const saved = await this.openDialog<unknown, string | boolean>(UnitEditDialog, {
      typeName: this.typeName(),
      id: this.id(),
    });

    if (saved === true) {
      this.changed.emit();
    }
  }

  protected async remove(): Promise<void> {
    const confirmed = await this.openDialog<unknown, boolean>(ConfirmDialog, {
      title: 'Удалить элемент?',
      message: `Элемент ${this.typeName()} будет удалён безвозвратно.`,
      confirmLabel: 'Удалить',
    });

    if (confirmed !== true) {
      return;
    }

    await this.run(async () => {
      await this.commands.delete(this.typeName(), this.id());
      this.deleted.emit(this.id());
      this.changed.emit();
    });
  }

  protected async exportUnit(): Promise<void> {
    await this.run(() => this.commands.export(this.typeName(), this.id()));
  }

  private openDialog<TData, TResult>(
    component: ComponentType<unknown>,
    data: TData,
  ): Promise<TResult | undefined> {
    const ref = this.dialog.open<unknown, TData, TResult>(component, {
      data,
      width: 'min(90vw, 640px)',
    });

    return firstValueFrom(ref.afterClosed());
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.isRunning.set(true);

    try {
      await action();
    } catch (error) {
      this.failed.emit(describeError(error));
    } finally {
      this.isRunning.set(false);
    }
  }
}
