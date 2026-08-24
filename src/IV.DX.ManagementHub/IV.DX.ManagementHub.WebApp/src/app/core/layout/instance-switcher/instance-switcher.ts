import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { InstancesService } from '@core/instances/instances.service';
// Type-only: erased at compile time, so it does not pull the dialog into this chunk.
import type { PicklistDialog as PicklistDialogComponent } from '@shared/ui/picklist-field/picklist-dialog';
import type {
  PicklistDialogData,
  PicklistOption,
  PicklistResult,
} from '@shared/ui/picklist-field/picklist-option';

/**
 * Shows and changes the DX instance the app works against.
 *
 * A button rather than `picklist-field`: a form field carries a floating label
 * and form-field height, which does not fit a toolbar. The chooser dialog is the
 * same one, so the behaviour matches the pickers used inside forms.
 *
 * Switching is a navigation, not a state change: the key lives in the URL, so the
 * same screen reopens under the other instance and every request refetches on its
 * own. Nothing has to be invalidated by hand.
 *
 * The chooser is imported on demand: this component lives in the shell, so a
 * static import would drag the dialog and its virtual scrolling into the eager
 * bundle for a screen most sessions never open.
 */
@Component({
  selector: 'mh-instance-switcher',
  imports: [MatButtonModule, MatIconModule],
  template: `
    @if (options().length > 0) {
      <button matButton type="button" aria-haspopup="dialog" (click)="choose()">
        {{ currentTitle() }}
        <mat-icon iconPositionEnd>arrow_drop_down</mat-icon>
      </button>
    }
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstanceSwitcher {
  private readonly instances = inject(InstancesService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly options = computed<readonly PicklistOption[]>(() =>
    this.instances.instances().map((instance) => ({ value: instance.key, label: instance.title })),
  );

  protected readonly currentTitle = computed(
    () => this.instances.current()?.title ?? this.instances.currentKey() ?? 'Instance',
  );

  protected async choose(): Promise<void> {
    const data: PicklistDialogData = {
      title: 'Instance',
      options: this.options(),
      value: this.instances.currentKey() ?? null,
      allowNull: false,
    };

    const { PicklistDialog } = await import('@shared/ui/picklist-field/picklist-dialog');

    const ref = this.dialog.open<PicklistDialogComponent, PicklistDialogData, PicklistResult>(
      PicklistDialog,
      { data, width: 'min(90vw, 420px)' },
    );

    const result = await firstValueFrom(ref.afterClosed());
    const key = result?.value;

    if (key === undefined || key === null || key === this.instances.currentKey()) {
      return;
    }

    void this.router.navigateByUrl(this.instances.targetUrl(key));
  }
}
