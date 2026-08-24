import { booleanAttribute, ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

/**
 * Short message in place of content: loading failed, nothing found, nothing to
 * show. Every screen needs the same three, so the wording stays in the caller
 * and only the shape lives here.
 */
@Component({
  selector: 'mh-notice',
  imports: [MatButtonModule],
  template: `
    <p class="notice__text" [class.notice__text--error]="tone() === 'error'">
      <ng-content />
    </p>

    @if (detail(); as detail) {
      <p class="notice__detail">{{ detail }}</p>
    }

    @if (retryable()) {
      <button matButton (click)="retry.emit()">Retry</button>
    }
  `,
  styles: `
    :host {
      display: block;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
    }

    p {
      margin: 0 0 0.75rem;
    }

    .notice__text--error {
      color: var(--mat-sys-error);
    }

    .notice__detail {
      font: var(--mat-sys-body-small);
      overflow-wrap: anywhere;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Notice {
  /** Technical detail under the message, usually what the server answered. */
  readonly detail = input<string | null>(null);
  readonly tone = input<'muted' | 'error'>('muted');
  /** `booleanAttribute` so the bare `retryable` attribute works. */
  readonly retryable = input(false, { transform: booleanAttribute });

  readonly retry = output<void>();
}
