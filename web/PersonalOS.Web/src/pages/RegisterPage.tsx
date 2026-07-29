import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, useHistory } from 'react-router-dom'
import { ProblemAlert } from '../components/ProblemAlert'
import { useRegister } from '../features/auth/hooks/useRegister'
import { registerSchema } from '../features/auth/schemas/registerSchema'
import type { RegisterFormValues } from '../features/auth/schemas/registerSchema'

export function RegisterPage() {
  const history = useHistory()
  const registerMutation = useRegister()
  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      displayName: '',
      email: '',
      password: '',
    },
  })

  async function onSubmit(values: RegisterFormValues) {
    try {
      await registerMutation.mutateAsync(values)
      history.replace('/login', { registered: true })
    } catch {
      // The mutation state renders a safe ProblemDetails message.
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-panel" aria-labelledby="register-title">
        <p className="eyebrow">PersonalOS</p>
        <h1 id="register-title">Crear cuenta</h1>

        {registerMutation.isError ? <ProblemAlert error={registerMutation.error} /> : null}

        <form className="form-stack" noValidate onSubmit={form.handleSubmit(onSubmit)}>
          <div className="field">
            <label htmlFor="displayName">Nombre</label>
            <input
              id="displayName"
              type="text"
              autoComplete="name"
              aria-invalid={Boolean(form.formState.errors.displayName)}
              aria-describedby={
                form.formState.errors.displayName ? 'displayName-error' : undefined
              }
              {...form.register('displayName')}
            />
            {form.formState.errors.displayName ? (
              <p className="field-error" id="displayName-error">
                {form.formState.errors.displayName.message}
              </p>
            ) : null}
          </div>

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
              autoComplete="new-password"
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

          <button
            className="button button-primary"
            type="submit"
            disabled={registerMutation.isPending || form.formState.isSubmitting}
          >
            {registerMutation.isPending ? 'Creando...' : 'Crear cuenta'}
          </button>
        </form>

        <p className="auth-link">
          Ya tienes cuenta? <Link to="/login">Iniciar sesion</Link>
        </p>
      </section>
    </main>
  )
}
