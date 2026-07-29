import { apiFetch } from '../../../lib/http/apiClient'
import { ApiError } from '../../../lib/problemDetails/problemDetails'
import type { CurrentUser, LoginRequest, RegisterRequest } from '../types'

export const currentUserQueryKey = ['auth', 'current-user'] as const

export async function getCurrentUser(): Promise<CurrentUser | null> {
  try {
    return await apiFetch<CurrentUser>('/api/auth/me')
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null
    }

    throw error
  }
}

export async function register(request: RegisterRequest): Promise<void> {
  await apiFetch('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export async function login(request: LoginRequest): Promise<CurrentUser> {
  return await apiFetch<CurrentUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export async function logout(): Promise<void> {
  await apiFetch('/api/auth/logout', {
    method: 'POST',
  })
}
