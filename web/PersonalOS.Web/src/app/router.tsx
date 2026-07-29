import { Redirect, Route, Switch } from 'react-router-dom'
import { AuthenticatedLayout } from '../components/AuthenticatedLayout'
import { ProtectedRoute } from '../features/auth/components/ProtectedRoute'
import { DashboardPage } from '../pages/DashboardPage'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'

export function AppRoutes() {
  return (
    <Switch>
      <Route exact path="/">
        <Redirect to="/dashboard" />
      </Route>

      <Route path="/register">
        <RegisterPage />
      </Route>

      <Route path="/login">
        <LoginPage />
      </Route>

      <ProtectedRoute path="/dashboard">
        <AuthenticatedLayout>
          <DashboardPage />
        </AuthenticatedLayout>
      </ProtectedRoute>

      <Route>
        <Redirect to="/dashboard" />
      </Route>
    </Switch>
  )
}
