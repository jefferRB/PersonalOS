import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AppProviders } from '../app/App'
import { createPersonalOSQueryClient } from '../app/queryClient'
import { AppRoutes } from '../app/router'

type RenderAppOptions = {
  route?: string
}

export function renderApp({ route = '/' }: RenderAppOptions = {}) {
  const queryClient = createPersonalOSQueryClient()

  return {
    queryClient,
    ...render(
      <AppProviders queryClient={queryClient}>
        <MemoryRouter initialEntries={[route]}>
          <AppRoutes />
        </MemoryRouter>
      </AppProviders>,
    ),
  }
}

export function renderWithProviders(ui: ReactElement) {
  const queryClient = createPersonalOSQueryClient()

  return {
    queryClient,
    ...render(<AppProviders queryClient={queryClient}>{ui}</AppProviders>),
  }
}
