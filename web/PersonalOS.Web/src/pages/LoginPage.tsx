import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, Redirect, useHistory, useLocation } from 'react-router-dom'
import type { Location } from 'history'
import { ProblemAlert } from '../components/ProblemAlert'
import { useCurrentUser } from '../features/auth/hooks/useCurrentUser'
import { useLogin } from '../features/auth/hooks/useLogin'
import { loginSchema } from '../features/auth/schemas/loginSchema'
import type { LoginFormValues } from '../features/auth/schemas/loginSchema'

type LoginLocationState = {
  registered?: boolean
  from?: Location
}

export function LoginPage() {
  const history = useHistory()
  const location = useLocation<LoginLocationState | undefined>()
  const currentUser = useCurrentUser()
  const loginMutation = useLogin()
  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  })

  if (currentUser.data) {
    return <Redirect to="/dashboard" />
  }

  async function onSubmit(values: LoginFormValues) {
    try {
      await loginMutation.mutateAsync(values)
      history.replace(location.state?.from?.pathname ?? '/dashboard')
    } catch {
      // The mutation state renders a safe ProblemDetails message.
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-panel" aria-labelledby="login-title">
        <p className="eyebrow">PersonalOS</p>
        <h1 id="login-title">Iniciar sesion</h1>

        {location.state?.registered ? (
          <div className="alert alert-success" role="status">
            Cuenta creada. Inicia sesion para continuar.
          </div>
        ) : null}

        {loginMutation.isError ? <ProblemAlert error={loginMutation.error} /> : null}

        <form className="form-stack" noValidate onSubmit={form.handleSubmit(onSubmit)}>
          <div className="field">
            <label htmlFor="email">Correo</label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              aria-invalid={Boolean(form.formState.errors.email)}
              aria-describedby={form.formState.errors.email ? 'email-error' : undefined}
              {...form.register('email')}
            />
            {form.formState.errors.email ? (
              <p className="field-error" id="email-error">
                {form.formState.errors.email.message}
              </p>
            ) : null}
          </div>

          <div className="field">
            <label htmlFor="password">Contrasena</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              aria-invalid={Boolean(form.formState.errors.password)}
              aria-describedby={form.formState.errors.password ? 'password-error' : undefined}
              {...form.register('password')}
            />
            {form.formState.errors.password ? (
              <p className="field-error" id="password-error">
                {form.formState.errors.password.message}
              </p>
            ) : null}
          </div>

          <label className="checkbox-field" htmlFor="rememberMe">
            <input id="rememberMe" type="checkbox" {...form.register('rememberMe')} />
            Mantener sesion iniciada
          </label>

          <button
            className="button button-primary"
            type="submit"
            disabled={loginMutation.isPending || form.formState.isSubmitting}
          >
            {loginMutation.isPending ? 'Entrando...' : 'Entrar'}
          </button>
        </form>

        <p className="auth-link">
          No tienes cuenta? <Link to="/register">Crear cuenta</Link>
        </p>
      </section>
    </main>
  )
}
