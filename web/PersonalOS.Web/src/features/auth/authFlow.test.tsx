import { cleanup, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { getCurrentUser } from './api/authApi'
import { renderApp } from '../../test/testUtils'

describe('auth flow', () => {
  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
    localStorage.clear()
    sessionStorage.clear()
  })

  it('validates the register form', async () => {
    mockFetch(async () => jsonResponse({ title: 'Unauthorized' }, 401))
    const user = userEvent.setup()

    renderApp({ route: '/register' })

    await user.click(screen.getByRole('button', { name: /crear cuenta/i }))

    expect(await screen.findByText(/nombre debe tener/i)).toBeInTheDocument()
    expect(screen.getByText(/correo valido/i)).toBeInTheDocument()
    expect(screen.getByText(/contrasena debe tener/i)).toBeInTheDocument()
  })

  it('validates the login form', async () => {
    mockFetch(async () => jsonResponse({ title: 'Unauthorized' }, 401))
    const user = userEvent.setup()

    renderApp({ route: '/login' })

    await user.click(screen.getByRole('button', { name: /entrar/i }))

    expect(await screen.findByText(/correo valido/i)).toBeInTheDocument()
    expect(screen.getByText(/contrasena debe tener/i)).toBeInTheDocument()
  })

  it('shows loading while ProtectedRoute resolves the session', () => {
    mockFetch(() => new Promise<Response>(() => undefined))

    renderApp({ route: '/dashboard' })

    expect(screen.getByText(/cargando sesion/i)).toBeInTheDocument()
  })

  it('redirects protected routes when there is no session', async () => {
    mockFetch(async () => jsonResponse({ title: 'Unauthorized' }, 401))

    renderApp({ route: '/dashboard' })

    expect(
      await screen.findByRole('heading', { name: /iniciar sesion/i }),
    ).toBeInTheDocument()
  })

  it('renders the dashboard with the authenticated user', async () => {
    mockFetch(async () =>
      jsonResponse({
        id: '2d896de6-02f7-48b7-b322-cc9f2ad63d5f',
        displayName: 'Jefferson',
        email: 'jefferson@example.com',
      }),
    )

    renderApp({ route: '/dashboard' })

    expect(
      await screen.findByRole('heading', {
        name: /personalos esta funcionando/i,
      }),
    ).toBeInTheDocument()
    expect(screen.getByText('Jefferson')).toBeInTheDocument()
    expect(screen.getByText('jefferson@example.com')).toBeInTheDocument()
  })

  it('shows ProblemDetails safely on login failure', async () => {
    mockFetch(async (input) => {
      const url = getUrl(input)

      if (url.endsWith('/api/antiforgery/token')) {
        return jsonResponse({ requestToken: 'request-token' })
      }

      if (url.endsWith('/api/auth/login')) {
        return jsonResponse(
          {
            title: 'Invalid credentials.',
            detail: 'The email or password is incorrect.',
            stack: 'internal stack should not render',
          },
          401,
        )
      }

      return jsonResponse({ title: 'Unauthorized' }, 401)
    })
    const user = userEvent.setup()

    renderApp({ route: '/login' })
    await fillLoginForm(user)
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    expect(
      await screen.findByText(/email or password is incorrect/i),
    ).toBeInTheDocument()
    expect(screen.queryByText(/internal stack/i)).not.toBeInTheDocument()
  })

  it('clears the authenticated state on logout', async () => {
    let loggedOut = false
    mockFetch(async (input) => {
      const url = getUrl(input)

      if (url.endsWith('/api/antiforgery/token')) {
        return jsonResponse({ requestToken: 'request-token' })
      }

      if (url.endsWith('/api/auth/logout')) {
        loggedOut = true
        return new Response(null, { status: 204 })
      }

      if (loggedOut) {
        return jsonResponse({ title: 'Unauthorized' }, 401)
      }

      return jsonResponse({
        id: '2d896de6-02f7-48b7-b322-cc9f2ad63d5f',
        displayName: 'Jefferson',
        email: 'jefferson@example.com',
      })
    })
    const user = userEvent.setup()

    renderApp({ route: '/dashboard' })

    await screen.findByRole('heading', { name: /personalos esta funcionando/i })
    await user.click(screen.getByRole('button', { name: /cerrar sesion/i }))

    expect(
      await screen.findByRole('heading', { name: /iniciar sesion/i }),
    ).toBeInTheDocument()
  })

  it('treats 401 current user responses as anonymous sessions', async () => {
    mockFetch(async () => jsonResponse({ title: 'Unauthorized' }, 401))

    await expect(getCurrentUser()).resolves.toBeNull()
  })

  it('shows a clear message for 429 responses', async () => {
    mockFetch(async (input) => {
      const url = getUrl(input)

      if (url.endsWith('/api/antiforgery/token')) {
        return jsonResponse({ requestToken: 'request-token' })
      }

      if (url.endsWith('/api/auth/login')) {
        return jsonResponse({ title: 'Too many requests.' }, 429)
      }

      return jsonResponse({ title: 'Unauthorized' }, 401)
    })
    const user = userEvent.setup()

    renderApp({ route: '/login' })
    await fillLoginForm(user)
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    expect(await screen.findByText(/demasiados intentos/i)).toBeInTheDocument()
  })

  it('does not write auth tokens to browser storage', async () => {
    const localStorageSpy = vi.spyOn(Storage.prototype, 'setItem')
    mockFetch(async (input) => {
      const url = getUrl(input)

      if (url.endsWith('/api/antiforgery/token')) {
        return jsonResponse({ requestToken: 'request-token' })
      }

      if (url.endsWith('/api/auth/login')) {
        return jsonResponse({
          id: '2d896de6-02f7-48b7-b322-cc9f2ad63d5f',
          displayName: 'Jefferson',
          email: 'jefferson@example.com',
        })
      }

      return jsonResponse({ title: 'Unauthorized' }, 401)
    })
    const user = userEvent.setup()

    renderApp({ route: '/login' })
    await fillLoginForm(user)
    await user.click(screen.getByRole('button', { name: /entrar/i }))

    await waitFor(() => {
      expect(screen.getByText('Jefferson')).toBeInTheDocument()
    })
    expect(localStorageSpy).not.toHaveBeenCalled()
  })
})

async function fillLoginForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/correo/i), 'jefferson@example.com')
  await user.type(screen.getByLabelText(/contrasena/i), 'Password123')
}

type FetchHandler = (
  input: RequestInfo | URL,
  init?: RequestInit,
) => Response | Promise<Response>

function mockFetch(handler: FetchHandler) {
  const fetchMock = vi.fn(handler)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function jsonResponse(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: {
      'Content-Type': 'application/json',
    },
  })
}

function getUrl(input: RequestInfo | URL) {
  return input instanceof Request ? input.url : String(input)
}
