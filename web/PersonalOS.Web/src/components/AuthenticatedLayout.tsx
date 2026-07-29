import type { ReactNode } from 'react'

type AuthenticatedLayoutProps = {
  children: ReactNode
}

export function AuthenticatedLayout({ children }: AuthenticatedLayoutProps) {
  return (
    <div className="app-shell">
      <header className="app-header">
        <a className="brand-link" href="/dashboard">
          PersonalOS
        </a>
      </header>
      <main className="app-main">{children}</main>
    </div>
  )
}
