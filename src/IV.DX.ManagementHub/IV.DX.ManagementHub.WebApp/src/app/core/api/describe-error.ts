import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  readonly detail?: unknown;
  readonly title?: unknown;
}

/**
 * Readable text for a failure.
 *
 * `HttpErrorResponse` does not extend `Error`, so `String(error)` on it yields
 * `[object Object]` and `instanceof Error` is false. The part worth showing is
 * usually the problem-details body the API sends — that is where messages like
 * "The update method for X isn't implemented yet" live.
 */
export function describeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body: unknown = error.error;

    if (typeof body === 'string' && body.trim() !== '') {
      return body;
    }

    if (typeof body === 'object' && body !== null) {
      const { detail, title } = body as ProblemDetails;

      for (const candidate of [detail, title]) {
        if (typeof candidate === 'string' && candidate.trim() !== '') {
          return candidate;
        }
      }
    }

    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return String(error);
}
