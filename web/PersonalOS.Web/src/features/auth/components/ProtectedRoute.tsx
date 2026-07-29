import type { ReactNode } from 'react'
import { Redirect, Route } from 'react-router-dom'
import type { RouteComponentProps, RouteProps } from 'react-router-dom'
import { ProblemAlert } from '../../../components/ProblemAlert'
import { useCurrentUser } from '../hooks/useCurrentUser'

type ProtectedRouteProps = RouteProps & {
  children: ReactNode
}

export function ProtectedRoute({ children, ...routeProps }: ProtectedRouteProps) {
  return (
    <Route
      {...routeProps}
      render={(route) => (
        <ProtectedContent route={route}>{children}</ProtectedContent>
      )}
    />
  )
}

type ProtectedContentProps = {
  children: ReactNode
  route: RouteComponentProps
}

function ProtectedContent({ children, route }: ProtectedContentProps) {
  const currentUser = useCurrentUser()

  if (currentUser.isPending) {
    return (
      <main className="centered-page" aria-live="polite">
        <p className="status-text">Cargando sesion...</p>
      </main>
    )
  }

  if (currentUser.isError) {
    return (
      <main className="centered-page">
        <ProblemAlert error={currentUser.error} />
      </main>
    )
  }

  if (!currentUser.data) {
    return (
      <Redirect
        to={{
          pathname: '/login',
          state: { from: route.location },
        }}
      />
    )
  }

  return <>{children}</>
}
