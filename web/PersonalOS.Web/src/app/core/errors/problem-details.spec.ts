import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';

import { formLevelMessage, normalizeApiError } from './problem-details';

describe('Problem Details normalization', () => {
  it('parses validation errors from application/problem+json safely', () => {
    const error = normalizeApiError(
      new HttpErrorResponse({
        status: 400,
        statusText: 'Bad Request',
        error: {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: {
            email: ['Email is required.'],
          },
          traceId: 'trace-1',
        },
      }),
    );

    expect(error.category).toBe('validation');
    expect(error.validationErrors['email']).toEqual(['Email is required.']);
    expect(error.traceId).toBe('trace-1');
    expect(formLevelMessage(error)).toBe('Review the highlighted fields and try again.');
  });

  it('represents rate-limit responses without rendering backend content as HTML', () => {
    const error = normalizeApiError(
      new HttpErrorResponse({
        status: 429,
        statusText: 'Too Many Requests',
        headers: new HttpHeaders({ 'Retry-After': '60 seconds' }),
        error: {
          title: 'Too many requests.',
          status: 429,
          detail: '<strong>Try later</strong>',
        },
      }),
    );

    expect(error.category).toBe('rateLimit');
    expect(error.detail).toBe('<strong>Try later</strong>');
    expect(formLevelMessage(error)).toBe('Too many attempts. Try again after 60 seconds.');
  });

  it('handles non-ProblemDetails network failures', () => {
    const error = normalizeApiError(
      new HttpErrorResponse({
        status: 0,
        statusText: 'Unknown Error',
        error: 'not-json',
      }),
    );

    expect(error.category).toBe('network');
    expect(error.validationErrors).toEqual({});
  });
});
