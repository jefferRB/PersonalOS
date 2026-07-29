import { z } from 'zod'

export const registerSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(2, 'El nombre debe tener al menos 2 caracteres.')
    .max(100, 'El nombre no puede superar 100 caracteres.'),
  email: z.string().trim().email('Ingresa un correo valido.'),
  password: z.string().min(8, 'La contrasena debe tener al menos 8 caracteres.'),
})

export type RegisterFormValues = z.infer<typeof registerSchema>
