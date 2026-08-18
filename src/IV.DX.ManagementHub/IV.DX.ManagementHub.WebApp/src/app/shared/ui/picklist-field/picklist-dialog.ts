import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';

import {
  filterOptions,
  type PicklistDialogData,
  type PicklistOption,
  type PicklistResult,
} from './picklist-option';

/** Height of one row; the virtual viewport needs it up front. */
const ITEM_SIZE = 48;

/**
 * Searchable chooser shown in a dialog.
 *
 * A relation column can offer hundreds of units, which is why the list is
 * filtered by typing and rendered through a virtual viewport rather than laid
 * out in full.
 */
@Component({
  selector: 'mh-picklist-dialog',
  imports: [
    ScrollingModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
  ],
  templateUrl: './picklist-dialog.html',
  styleUrl: './picklist-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PicklistDialog {
  private readonly dialogRef = inject(MatDialogRef<PicklistDialog, PicklistResult>);

  protected readonly data = inject<PicklistDialogData>(MAT_DIALOG_DATA);
  protected readonly itemSize = ITEM_SIZE;

  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  protected readonly search = signal('');

  constructor() {
    // In a list of hundreds the current choice is usually outside the rendered
    // window, so bring it into view instead of making the user hunt for it.
    //
    // This waits for the dialog to finish opening: before that the viewport has
    // no measured size, and scrolling a viewport of height zero does nothing.
    this.dialogRef
      .afterOpened()
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        const viewport = this.viewport();
        const index = this.data.options.findIndex((option) => option.value === this.data.value);

        viewport?.checkViewportSize();

        if (index > 0) {
          viewport?.scrollToIndex(index, 'auto');
        }
      });
  }

  protected readonly filtered = computed(() => filterOptions(this.data.options, this.search()));

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected isSelected(value: string | null): boolean {
    return (this.data.value ?? null) === value;
  }

  /** `*cdkVirtualFor` needs a plain trackBy; the value is already unique. */
  protected trackByValue(_index: number, option: PicklistOption): string {
    return option.value;
  }

  protected select(value: string | null): void {
    this.dialogRef.close({ value });
  }
}
