import { useMutation } from '@tanstack/react-query'
import { register } from '../api/authApi'
import type { RegisterRequest } from '../types'

export function useRegister() {
  return useMutation<void, Error, RegisterRequest>({
    mutationFn: register,
  })
}
