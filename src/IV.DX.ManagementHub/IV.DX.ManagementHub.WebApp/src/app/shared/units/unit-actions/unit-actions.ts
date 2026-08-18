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
  /** Records to act on. Editing needs exactly one; the rest work on many. */
  readonly ids = input.required<readonly string[]>();

  readonly canEdit = input(false);
  readonly canDelete = input(false);
  readonly canExport = input(false);

  /** Labels next to the icons; the panel uses them, table rows do not. */
  readonly showLabels = input(false);

  /** The records were saved; rows need reloading. */
  readonly changed = output<void>();
  /** The records are gone; whoever shows them has to let go of the ids. */
  readonly deleted = output<readonly string[]>();
  readonly failed = output<string>();

  protected readonly isRunning = signal(false);

  /** Editing addresses one record, so it is offered only for a single selection. */
  private readonly canEditOne = computed(() => this.canEdit() && this.ids().length === 1);
  private readonly hasSelection = computed(() => this.ids().length > 0);

  /**
   * Actions to render, in a fixed order. Built here rather than spelled out three
   * times in the template, which is what made the icon-only and labelled variants
   * drift apart.
   */
  protected readonly actions = computed(() =>
    [
      { icon: 'edit', label: 'Изменить', visible: this.canEditOne(), run: () => this.edit() },
      {
        icon: 'download',
        label: 'Экспорт',
        visible: this.canExport() && this.hasSelection(),
        run: () => this.exportUnit(),
      },
      {
        icon: 'delete',
        label: 'Удалить',
        visible: this.canDelete() && this.hasSelection(),
        run: () => this.remove(),
      },
    ].filter((action) => action.visible),
  );

  protected async edit(): Promise<void> {
    const saved = await this.openDialog<unknown, string | boolean>(UnitEditDialog, {
      typeName: this.typeName(),
      id: this.ids()[0],
    });

    if (saved === true) {
      this.changed.emit();
    }
  }

  protected async remove(): Promise<void> {
    const ids = this.ids();
    const count = ids.length;

    const confirmed = await this.openDialog<unknown, boolean>(ConfirmDialog, {
      title: count === 1 ? 'Удалить элемент?' : `Удалить элементы (${count})?`,
      message:
        count === 1
          ? `Элемент ${this.typeName()} будет удалён безвозвратно.`
          : `Записей ${this.typeName()}: ${count}. Они будут удалены безвозвратно.`,
      confirmLabel: 'Удалить',
    });

    if (confirmed !== true) {
      return;
    }

    await this.run(async () => {
      await this.commands.deleteMany(this.typeName(), ids);
      this.deleted.emit(ids);
      this.changed.emit();
    });
  }

  protected async exportUnit(): Promise<void> {
    await this.run(() => this.commands.export(this.typeName(), this.ids()));
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
