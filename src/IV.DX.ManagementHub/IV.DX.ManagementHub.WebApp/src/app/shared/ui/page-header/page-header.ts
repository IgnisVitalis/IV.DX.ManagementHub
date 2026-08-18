import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Title bar shared by feature pages. Presentational only — no injected state. */
@Component({
  selector: 'mh-page-header',
  imports: [],
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
