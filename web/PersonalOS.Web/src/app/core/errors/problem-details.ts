import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';

export type ApiErrorCategory =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'conflict'
  | 'rateLimit'
  | 'locked'
  | 'server'
  | 'network'
  | 'unknown';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  [extension: string]: unknown;
}

export interface ApiError {
  readonly status: number;
  readonly category: ApiErrorCategory;
  readonly title: string;
  readonly detail: string;
  readonly validationErrors: Record<string, string[]>;
  readonly retryAfter: string | null;
  readonly traceId: string | null;
}

export function normalizeApiError(error: unknown): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return createApiError({
      status: 0,
      category: 'unknown',
      title: 'Unexpected error.',
      detail: 'The request could not be completed.',
    });
  }

  if (error.status === 0) {
    return createApiError({
      status: 0,
      category: 'network',
      title: 'Network unavailable.',
      detail: 'PersonalOS could not reach the server. Check the API and try again.',
      retryAfter: readRetryAfter(error.headers),
    });
  }

  const problem = readProblemDetails(error.error);
  const status = problem.status ?? error.status;
  const category = categoryForStatus(status, problem);

  return createApiError({
    status,
    category,
    title: problem.title ?? titleForCategory(category),
    detail: problem.detail ?? detailForCategory(category),
    validationErrors: problem.errors ?? {},
    retryAfter: readRetryAfter(error.headers),
    traceId: readTraceId(problem),
  });
}

export function isApiError(error: unknown): error is ApiError {
  return isRecord(error)
    && typeof error['status'] === 'number'
    && typeof error['category'] === 'string'
    && typeof error['title'] === 'string'
    && typeof error['detail'] === 'string';
}

export function isUnauthorizedError(error: unknown): boolean {
  return isApiError(error) && error.category === 'unauthorized';
}

export function formLevelMessage(error: ApiError): string {
  switch (error.category) {
    case 'validation':
      return 'Review the highlighted fields and try again.';
    case 'unauthorized':
      return 'The email or password is incorrect.';
    case 'forbidden':
      return 'You do not have permission to complete this action.';
    case 'conflict':
      return error.detail;
    case 'rateLimit':
      return error.retryAfter
        ? `Too many attempts. Try again after ${error.retryAfter}.`
        : 'Too many attempts. Wait a moment and try again.';
    case 'locked':
      return 'Too many failed attempts. Try again later.';
    case 'network':
      return error.detail;
    case 'server':
      return 'PersonalOS could not complete the request. Try again later.';
    case 'unknown':
      return 'The request could not be completed. Try again.';
  }
}

export function firstValidationError(
  errors: Record<string, string[]>,
  fieldName: string,
): string | null {
  const messages = errors[fieldName];

  return messages?.find((message) => message.trim().length > 0) ?? null;
}

function createApiError(options: {
  status: number;
  category: ApiErrorCategory;
  title: string;
  detail: string;
  validationErrors?: Record<string, string[]>;
  retryAfter?: string | null;
  traceId?: string | null;
}): ApiError {
  return {
    status: options.status,
    category: options.category,
    title: options.title,
    detail: options.detail,
    validationErrors: options.validationErrors ?? {},
    retryAfter: options.retryAfter ?? null,
    traceId: options.traceId ?? null,
  };
}

function readProblemDetails(value: unknown): ProblemDetails {
  if (!isRecord(value)) {
    return {};
  }

  return {
    type: readString(value, 'type'),
    title: readString(value, 'title'),
    status: readNumber(value, 'status'),
    detail: readString(value, 'detail'),
    instance: readString(value, 'instance'),
    traceId: readString(value, 'traceId'),
    errors: readValidationErrors(value),
  };
}

function readValidationErrors(value: Record<string, unknown>): Record<string, string[]> {
  const errors = value['errors'];

  if (!isRecord(errors)) {
    return {};
  }

  return Object.entries(errors).reduce<Record<string, string[]>>((mapped, [key, messages]) => {
    if (Array.isArray(messages)) {
      const safeMessages = messages.filter((message): message is string => typeof message === 'string');

      if (safeMessages.length > 0) {
        mapped[key] = safeMessages;
      }
    }

    return mapped;
  }, {});
}

function categoryForStatus(status: number, problem: ProblemDetails): ApiErrorCategory {
  if (status === 400 && problem.errors && Object.keys(problem.errors).length > 0) {
    return 'validation';
  }

  if (status === 401) {
    return 'unauthorized';
  }

  if (status === 403) {
    return 'forbidden';
  }

  if (status === 409) {
    return 'conflict';
  }

  if (status === 423) {
    return 'locked';
  }

  if (status === 429) {
    return 'rateLimit';
  }

  if (status >= 500) {
    return 'server';
  }

  return 'unknown';
}

function titleForCategory(category: ApiErrorCategory): string {
  switch (category) {
    case 'validation':
      return 'Validation failed.';
    case 'unauthorized':
      return 'Unauthorized.';
    case 'forbidden':
      return 'Forbidden.';
    case 'conflict':
      return 'Conflict.';
    case 'rateLimit':
      return 'Too many requests.';
    case 'locked':
      return 'Account temporarily locked.';
    case 'server':
      return 'Server error.';
    case 'network':
      return 'Network unavailable.';
    case 'unknown':
      return 'Request failed.';
  }
}

function detailForCategory(category: ApiErrorCategory): string {
  switch (category) {
    case 'validation':
      return 'One or more validation errors occurred.';
    case 'unauthorized':
      return 'Authentication is required.';
    case 'forbidden':
      return 'Access is denied.';
    case 'conflict':
      return 'The request conflicts with the current state.';
    case 'rateLimit':
      return 'Too many attempts. Try again later.';
    case 'locked':
      return 'Too many failed attempts. Try again later.';
    case 'server':
      return 'The server could not complete the request.';
    case 'network':
      return 'The server could not be reached.';
    case 'unknown':
      return 'The request could not be completed.';
  }
}

function readRetryAfter(headers: HttpHeaders): string | null {
  return headers.get('Retry-After');
}

function readTraceId(problem: ProblemDetails): string | null {
  return typeof problem.traceId === 'string' ? problem.traceId : null;
}

function readString(value: Record<string, unknown>, key: string): string | undefined {
  const candidate = value[key];

  return typeof candidate === 'string' && candidate.trim().length > 0 ? candidate : undefined;
}

function readNumber(value: Record<string, unknown>, key: string): number | undefined {
  const candidate = value[key];

  return typeof candidate === 'number' ? candidate : undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
