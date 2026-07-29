import { useMutation, useQueryClient } from '@tanstack/react-query'
import { currentUserQueryKey, login } from '../api/authApi'
import type { CurrentUser, LoginRequest } from '../types'

export function useLogin() {
  const queryClient = useQueryClient()

  return useMutation<CurrentUser, Error, LoginRequest>({
    mutationFn: login,
    onSuccess: (currentUser) => {
      queryClient.setQueryData(currentUserQueryKey, currentUser)
    },
  })
}
