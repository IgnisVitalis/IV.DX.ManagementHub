import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';

import { errorMessage } from '@core/api/resource';
import { Notice } from '@shared/ui/notice/notice';
import { UnitActions } from '@shared/units/unit-actions/unit-actions';
import {
  UnitEditDialog,
  type UnitEditDialogData,
} from '@shared/units/unit-edit-dialog/unit-edit-dialog';
import { CardViewService } from '../../services/card-view.service';

/** Records of one unit definition shown as cards. */
@Component({
  selector: 'mh-card-view-page',
  imports: [
    Notice,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    UnitActions,
  ],
  templateUrl: './card-view-page.html',
  styleUrl: './card-view-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CardViewService],
})
export class CardViewPage {
  private readonly view = inject(CardViewService);
  private readonly dialog = inject(MatDialog);

  protected readonly isLoading = this.view.isLoading;
  protected readonly isUnresolved = this.view.isUnresolved;
  protected readonly definition = this.view.definition;
  protected readonly cards = this.view.cards;

  protected readonly actionError = signal<string | null>(null);

  protected readonly error = errorMessage(this.view.error);

  protected readonly isEmpty = computed(
    () => !this.isLoading() && !this.error() && this.cards().items.length === 0,
  );

  protected readonly canCreate = computed(() => this.definition()?.isCreatable ?? false);
  protected readonly canEdit = computed(() => this.definition()?.isEditable ?? false);
  protected readonly canDelete = computed(() => this.definition()?.isDeletable ?? false);
  protected readonly canExport = computed(() => this.definition()?.isExportable ?? false);

  protected reload(): void {
    this.actionError.set(null);
    this.view.reload();
  }

  protected onFailed(message: string): void {
    this.actionError.set(message);
  }

  protected async create(): Promise<void> {
    const data: UnitEditDialogData = { typeName: this.cards().typeName };
    const ref = this.dialog.open<UnitEditDialog, UnitEditDialogData, string | boolean>(
      UnitEditDialog,
      { data, width: 'min(90vw, 640px)' },
    );

    if (typeof (await firstValueFrom(ref.afterClosed())) === 'string') {
      this.reload();
    }
  }
}
