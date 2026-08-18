import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import type { EditorField } from '@core/units/editor-field';
import type { EditValue } from '@core/units/unit-record.patch';
import { PicklistField } from '@shared/ui/picklist-field/picklist-field';

/**
 * One editable column. Shared by the main element and by collection rows, so a
 * column of a given type looks and behaves the same wherever it appears.
 */
@Component({
  selector: 'mh-unit-field',
  imports: [MatCheckboxModule, MatFormFieldModule, MatInputModule, PicklistField],
  templateUrl: './unit-field.html',
  styleUrl: './unit-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitField {
  readonly field = input.required<EditorField>();
  readonly value = model<EditValue>(null);
  /** Shows the "required" message under the control. */
  readonly invalid = input(false);

  /**
   * Picklist options are keyed by string, while an enum-backed column holds a
   * number — comparing the two directly is why the current choice used to show
   * up as empty. Everything crossing that boundary is normalised to text; the
   * numeric type is restored on save by `toWireValue`.
   */
  protected readonly pickValue = computed(() => {
    const value = this.value();
    return value === null || value === '' ? null : String(value);
  });

  protected setFromInput(event: Event): void {
    this.value.set((event.target as HTMLInputElement | HTMLTextAreaElement).value);
  }
}
