import { InjectionToken } from '@angular/core';
import { environment } from '@env/environment';

/** Application-wide settings resolved at build time from the environment files. */
export interface AppConfig {
  readonly production: boolean;
  /** Base URL of the ManagementHub Web API, e.g. `/api`. */
  readonly apiBaseUrl: string;
  readonly appName: string;
  /**
   * Id of the DX query returning the navigation items. Configuration rather than
   * a constant: DX regenerates these ids when metadata is re-seeded, and a stale
   * one shows up as an empty menu instead of an error.
   */
  readonly navItemsQueryId: string;
  /**
   * Id of the `DXPCardViewUnit` listing the DX instances.
   *
   * No navigation metadata points at it — the Blazor app links to it from a
   * hardcoded menu entry too — so the id lives in configuration rather than in
   * a component, where a stale value would be far harder to spot.
   */
  readonly instancesCardViewId: string;
  /**
   * Key of the instance that is the hub itself.
   *
   * Hub-owned metadata — the instances card view among it — exists only in this
   * instance's database; a remote DX instance answers 404 for it. So screens
   * about the hub are opened in this instance, and it is also the default
   * landing point. Seeded together with the card view in
   * `01_01_0001_MH_MHInstanceUnit.dx`.
   */
  readonly hubInstanceKey: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG', {
  providedIn: 'root',
  factory: () => environment,
});
