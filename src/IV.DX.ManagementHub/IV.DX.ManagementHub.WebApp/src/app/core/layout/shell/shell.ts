import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';

import { APP_CONFIG } from '@core/config/app-config';
import { InstanceSwitcher } from '@core/layout/instance-switcher/instance-switcher';
import { NavMenu } from '@core/layout/nav-menu/nav-menu';

/**
 * Application shell: toolbar + collapsible side navigation around the routed
 * feature pages. Feature routes are registered as children of this component.
 */
@Component({
  selector: 'mh-shell',
  imports: [
    RouterOutlet,
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatToolbarModule,
    InstanceSwitcher,
    NavMenu,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  protected readonly appName = inject(APP_CONFIG).appName;
  protected readonly sidenavOpened = signal(true);

  protected toggleSidenav(): void {
    this.sidenavOpened.update((opened) => !opened);
  }
}
