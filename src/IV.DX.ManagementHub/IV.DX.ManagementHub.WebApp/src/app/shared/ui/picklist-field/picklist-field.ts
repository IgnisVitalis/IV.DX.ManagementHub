import { ChangeDetectionStrategy, Component, computed, inject, input, model } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { firstValueFrom } from 'rxjs';

import { PicklistDialog } from './picklist-dialog';
import type { PicklistDialogData, PicklistOption, PicklistResult } from './picklist-option';

/**
 * Chooser for a value out of a long list of named objects.
 *
 * A plain `mat-select` drops a hundreds-long list into an overlay with no way to
 * search it. This opens a dialog with a filter instead, while keeping the look
 * of a form field so it sits naturally among the other inputs.
 */
@Component({
  selector: 'mh-picklist-field',
  imports: [MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './picklist-field.html',
  styleUrl: './picklist-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PicklistField {
  private readonly dialog = inject(MatDialog);

  readonly label = input.required<string>();
  readonly options = input.required<readonly PicklistOption[]>();
  readonly value = model<string | null>(null);
  /** Offers an explicit "clear" entry in the dialog. */
  readonly allowNull = input(false);
  readonly placeholder = input('—');

  /** Label of the selected option, falling back to the placeholder. */
  protected readonly text = computed(() => {
    const value = this.value();

    if (value === null || value === '') {
      return this.placeholder();
    }

    return this.options().find((option) => option.value === value)?.label ?? value;
  });

  protected async open(): Promise<void> {
    const data: PicklistDialogData = {
      title: this.label(),
      options: this.options(),
      value: this.value(),
      allowNull: this.allowNull(),
    };

    const ref = this.dialog.open<PicklistDialog, PicklistDialogData, PicklistResult>(
      PicklistDialog,
      { data, width: 'min(90vw, 520px)', autoFocus: 'first-heading' },
    );

    const result = await firstValueFrom(ref.afterClosed());

    // Closing without a result means cancelled; only a real pick changes state.
    if (result !== undefined) {
      this.value.set(result.value);
    }
  }
}
