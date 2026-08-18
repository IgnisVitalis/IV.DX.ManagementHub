import type { AppConfig } from '@core/config/app-config';

export const environment: AppConfig = {
  production: true,
  /**
   * Relative by design: in production the SPA is served from the same origin
   * as the ManagementHub API, so no absolute host is needed.
   */
  apiBaseUrl: '/api',
  appName: 'IV.DX Management Hub',
  navItemsQueryId: '018fa54b-7fce-73b1-95d1-9c09aa4cc871',
  instancesCardViewId: 'f0b1c2d3-4e5f-4a6b-7c8d-9e0f1a2b3c4d',
  hubInstanceKey: 'Own',
};
