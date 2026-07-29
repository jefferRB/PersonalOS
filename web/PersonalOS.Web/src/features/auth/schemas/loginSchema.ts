import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().trim().email('Ingresa un correo valido.'),
  password: z.string().min(8, 'La contrasena debe tener al menos 8 caracteres.'),
  rememberMe: z.boolean(),
})

export type LoginFormValues = z.infer<typeof loginSchema>
