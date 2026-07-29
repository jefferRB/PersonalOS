import { useHistory } from 'react-router-dom'
import { ProblemAlert } from '../components/ProblemAlert'
import { useCurrentUser } from '../features/auth/hooks/useCurrentUser'
import { useLogout } from '../features/auth/hooks/useLogout'

export function DashboardPage() {
  const history = useHistory()
  const currentUser = useCurrentUser()
  const logoutMutation = useLogout()
  const user = currentUser.data

  async function handleLogout() {
    try {
      await logoutMutation.mutateAsync()
      history.replace('/login')
    } catch {
      // The mutation state renders a safe ProblemDetails message.
    }
  }

  if (!user) {
    return null
  }

  return (
    <section className="dashboard-section" aria-labelledby="dashboard-title">
      <div className="dashboard-copy">
        <p className="eyebrow">Dashboard</p>
        <h1 id="dashboard-title">PersonalOS esta funcionando</h1>
      </div>

      <dl className="user-summary">
        <div>
          <dt>Nombre</dt>
          <dd>{user.displayName}</dd>
        </div>
        <div>
          <dt>Correo</dt>
          <dd>{user.email}</dd>
        </div>
      </dl>

      {logoutMutation.isError ? <ProblemAlert error={logoutMutation.error} /> : null}

      <button
        className="button button-secondary"
        type="button"
        disabled={logoutMutation.isPending}
        onClick={handleLogout}
      >
        {logoutMutation.isPending ? 'Cerrando...' : 'Cerrar sesion'}
      </button>
    </section>
  )
}
