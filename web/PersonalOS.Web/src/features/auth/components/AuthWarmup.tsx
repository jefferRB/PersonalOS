import { useCurrentUser } from '../hooks/useCurrentUser'

export function AuthWarmup() {
  useCurrentUser()
  return null
}
