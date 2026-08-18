import type { AppConfig } from '@core/config/app-config';

export const environment: AppConfig = {
  production: false,
  /**
   * Still relative: `ng serve` proxies `/api` to the ASP.NET host
   * (see proxy.conf.mjs), which keeps cookies and CORS out of the picture.
   */
  apiBaseUrl: '/api',
  appName: 'IV.DX Management Hub (dev)',
  navItemsQueryId: '018fa54b-7fce-73b1-95d1-9c09aa4cc871',
  instancesCardViewId: 'f0b1c2d3-4e5f-4a6b-7c8d-9e0f1a2b3c4d',
  hubInstanceKey: 'Own',
};
