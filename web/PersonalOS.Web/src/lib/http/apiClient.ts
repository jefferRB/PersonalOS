import { ApiError } from '../problemDetails/problemDetails'

type ApiFetchOptions = RequestInit

const unsafeMethods = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])

export async function apiFetch<TResponse = void>(
  path: string,
  options: ApiFetchOptions = {},
): Promise<TResponse> {
  const method = (options.method ?? 'GET').toUpperCase()
  const headers = new Headers(options.headers)

  headers.set('Accept', 'application/json')

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (unsafeMethods.has(method)) {
    const requestToken = await getAntiforgeryToken()
    headers.set('X-XSRF-TOKEN', requestToken)
  }

  const response = await fetch(path, {
    ...options,
    method,
    headers,
    credentials: 'include',
  })

  if (!response.ok) {
    throw await ApiError.fromResponse(response)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}

async function getAntiforgeryToken() {
  const response = await fetch('/api/antiforgery/token', {
    credentials: 'include',
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw await ApiError.fromResponse(response)
  }

  const payload = (await response.json()) as { requestToken?: string }

  if (!payload.requestToken) {
    throw new ApiError(response.status, {
      title: 'Request verification failed.',
      status: response.status,
    })
  }

  return payload.requestToken
}
