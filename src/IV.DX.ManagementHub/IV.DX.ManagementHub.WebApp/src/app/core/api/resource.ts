import { computed, type Signal } from '@angular/core';

import { describeError } from './describe-error';

/** The slice of a resource these helpers read. */
interface ReadableResource<TValue> {
  hasValue(): boolean;
  value(): TValue;
}

/**
 * Value of a resource, or the fallback while it has none.
 *
 * Always go through this instead of reading `value()` directly: that signal
 * throws a `ResourceValueError` while the resource is in an error state, and an
 * unguarded read takes down the template rendering the error — leaving the
 * screen stuck on its spinner. `hasValue()` is the check that never throws.
 */
export function resourceValue<TValue>(
  resource: ReadableResource<TValue>,
  fallback: TValue,
): Signal<TValue> {
  return computed(() => (resource.hasValue() ? resource.value() : fallback));
}

/** Readable text of a failure, or `null` when there is none. */
export function errorMessage(error: Signal<Error | undefined>): Signal<string | null> {
  return computed(() => {
    const value = error();

    return value === undefined ? null : describeError(value);
  });
}
