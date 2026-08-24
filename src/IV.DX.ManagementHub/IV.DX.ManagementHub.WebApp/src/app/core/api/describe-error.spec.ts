import { HttpErrorResponse } from '@angular/common/http';

import { describeError } from './describe-error';

describe('describeError', () => {
  it('prefers the problem-details message the API sends', () => {
    const error = new HttpErrorResponse({
      status: 500,
      url: '/api/management/DXElementDefinitionUnit/1',
      error: {
        title: 'Internal server error',
        status: 500,
        detail: "The update method for DXRelationDefinitionUnit isn't implemented yet",
      },
    });

    expect(describeError(error)).toBe(
      "The update method for DXRelationDefinitionUnit isn't implemented yet",
    );
  });

  it('falls back to the title, then to the HTTP message', () => {
    expect(
      describeError(new HttpErrorResponse({ status: 404, error: { title: 'Not Found' } })),
    ).toBe('Not Found');

    expect(describeError(new HttpErrorResponse({ status: 404, error: null }))).toContain('404');
  });

  it('never renders an HttpErrorResponse as [object Object]', () => {
    // It does not extend Error, so String() on it is useless.
    const error = new HttpErrorResponse({ status: 500, error: {} });

    expect(describeError(error)).not.toContain('[object Object]');
  });

  it('handles plain errors and anything else', () => {
    expect(describeError(new Error('boom'))).toBe('boom');
    expect(describeError('plain text')).toBe('plain text');
  });
});
