import { useMutation, useQueryClient } from '@tanstack/react-query'
import { currentUserQueryKey, logout } from '../api/authApi'

export function useLogout() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: logout,
    onSuccess: () => {
      queryClient.removeQueries()
      queryClient.setQueryData(currentUserQueryKey, null)
    },
  })
}
