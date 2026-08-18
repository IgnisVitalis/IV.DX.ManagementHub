import { inject } from '@angular/core';
import { APP_CONFIG } from '@core/config/app-config';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router, type CanActivateFn, type UrlTree } from '@angular/router';
import { filter, firstValueFrom } from 'rxjs';

import { InstancesService } from './instances.service';

/**
 * Waits for the instance list to settle.
 *
 * Reading `instances()` first is deliberate: the resource is lazy, so touching it
 * is what starts the request — waiting on the status without reading would
 * observe the idle state and return immediately.
 */
async function loadedInstances(instances: InstancesService) {
  instances.instances();

  await firstValueFrom(toObservable(instances.isReady).pipe(filter((ready) => ready)));

  return instances.instances();
}

/**
 * Sends the bare root to the hub's own instance, falling back to whatever is
 * available. The hub is the natural landing point: it is the only instance whose
 * database holds the hub's own screens.
 */
export const defaultInstanceGuard: CanActivateFn = async (): Promise<boolean | UrlTree> => {
  const instances = inject(InstancesService);
  const router = inject(Router);
  const hubKey = inject(APP_CONFIG).hubInstanceKey;

  const available = await loadedInstances(instances);
  const target = available.find((instance) => instance.key === hubKey) ?? available[0];

  return target === undefined ? true : router.parseUrl(`/app/${target.key}`);
};

/**
 * Refuses an unknown instance key.
 *
 * Without this the app would happily render screens whose every request answers
 * 404, which reads as "the data is gone" rather than "this instance does not
 * exist".
 */
export const instanceGuard: CanActivateFn = async (route): Promise<boolean | UrlTree> => {
  const instances = inject(InstancesService);
  const router = inject(Router);
  const hubKey = inject(APP_CONFIG).hubInstanceKey;

  const key = route.paramMap.get('instanceKey');
  const available = await loadedInstances(instances);

  if (key !== null && available.some((instance) => instance.key === key)) {
    return true;
  }

  const fallback = available.find((instance) => instance.key === hubKey) ?? available[0];

  return fallback === undefined ? true : router.parseUrl(`/app/${fallback.key}`);
};
