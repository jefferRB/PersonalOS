export type CurrentUser = {
  id: string
  displayName: string
  email: string
}

export type RegisterRequest = {
  displayName: string
  email: string
  password: string
}

export type LoginRequest = {
  email: string
  password: string
  rememberMe: boolean
}
