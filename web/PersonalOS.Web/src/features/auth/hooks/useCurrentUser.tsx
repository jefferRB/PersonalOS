import { useQuery } from '@tanstack/react-query'
import { currentUserQueryKey, getCurrentUser } from '../api/authApi'

export function useCurrentUser() {
  return useQuery({
    queryKey: currentUserQueryKey,
    queryFn: getCurrentUser,
  })
}
