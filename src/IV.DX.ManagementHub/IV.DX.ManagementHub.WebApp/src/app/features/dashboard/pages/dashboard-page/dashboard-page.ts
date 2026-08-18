import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

import { PageHeader } from '@shared/ui/page-header/page-header';

@Component({
  selector: 'mh-dashboard-page',
  imports: [MatCardModule, PageHeader],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {}
