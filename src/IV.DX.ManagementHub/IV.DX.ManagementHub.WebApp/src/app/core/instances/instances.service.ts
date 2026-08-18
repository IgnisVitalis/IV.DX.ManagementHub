import { Injectable, computed, inject } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, type ActivatedRouteSnapshot } from '@angular/router';
import { filter } from 'rxjs';

import { APP_CONFIG } from '@core/config/app-config';
import { toInstances } from './instances.mapper';
import type { Instance } from './models/instance';

/** Route parameter carrying the instance key. */
export const INSTANCE_KEY_PARAM = 'instanceKey';

function findParam(route: ActivatedRouteSnapshot | null, name: string): string | undefined {
  for (let current = route; current !== null; current = current.firstChild) {
    const value = current.paramMap.get(name);

    if (value !== null) {
      return value;
    }
  }

  return undefined;
}

/**
 * The DX instance the app is currently working against.
 *
 * The URL is the single source of truth: the key lives in the route, so a link
 * carries its context, a reload keeps it, and two tabs can show two instances at
 * once. Everything else — the API prefix, the switcher — is derived from it.
 *
 * The list of instances itself is hub-local data (`MHInstanceUnit` lives in the
 * hub's own database), so it is read through the non-scoped management route
 * rather than through the instance prefix.
 */
@Injectable({ providedIn: 'root' })
export class InstancesService {
  private readonly config = inject(APP_CONFIG);
  private readonly router = inject(Router);

  private readonly navigated = toSignal(
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)),
  );

  private readonly resource = httpResource(
    () => `${this.config.apiBaseUrl}/management/MHInstanceUnit`,
    { parse: toInstances, defaultValue: [] },
  );

  readonly instances = computed<readonly Instance[]>(() =>
    this.resource.hasValue() ? this.resource.value() : [],
  );

  readonly isLoading = this.resource.isLoading;
  readonly error = this.resource.error;

  /** The list has settled, successfully or not — safe for a guard to act on. */
  readonly isReady = computed(() => {
    const status = this.resource.status();

    return status === 'resolved' || status === 'error' || status === 'local';
  });

  /** Key from the current route, or `undefined` outside an instance context. */
  readonly currentKey = computed(() => {
    this.navigated();

    return findParam(this.router.routerState.snapshot.root, INSTANCE_KEY_PARAM);
  });

  readonly current = computed(() => {
    const key = this.currentKey();

    return this.instances().find((instance) => instance.key === key);
  });

  /**
   * Prefix for every instance-scoped API call. The instance travels in the path
   * rather than in a header because HTTP caches key on the URL.
   */
  readonly apiBase = computed(() => {
    const key = this.currentKey();

    return key === undefined ? undefined : `${this.config.apiBaseUrl}/i/${key}`;
  });

  /** Route to the same screen in another instance, mirroring the Blazor switcher. */
  targetUrl(key: string): string {
    const segments = this.router.url.split('/').filter((segment) => segment !== '');

    if (segments[0] === 'app' && segments.length >= 2) {
      segments[1] = key;
      return `/${segments.join('/')}`;
    }

    return `/app/${key}`;
  }

  reload(): void {
    this.resource.reload();
  }
}
