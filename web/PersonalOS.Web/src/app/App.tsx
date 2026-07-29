import type { ReactNode } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import type { QueryClient } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { AuthWarmup } from '../features/auth/components/AuthWarmup'
import { AppRoutes } from './router'
import { queryClient as defaultQueryClient } from './queryClient'

type AppProvidersProps = {
  children: ReactNode
  queryClient: QueryClient
}

export function AppProviders({ children, queryClient }: AppProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthWarmup />
      {children}
    </QueryClientProvider>
  )
}

type AppProps = {
  queryClient?: QueryClient
}

export function App({ queryClient = defaultQueryClient }: AppProps) {
  return (
    <AppProviders queryClient={queryClient}>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AppProviders>
  )
}

export default App
