export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${status}.`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  static async fromResponse(response: Response) {
    const contentType = response.headers.get('content-type') ?? ''

    if (contentType.includes('json')) {
      try {
        const problem = (await response.json()) as ProblemDetails
        return new ApiError(response.status, problem)
      } catch {
        return new ApiError(response.status)
      }
    }

    return new ApiError(response.status)
  }
}

export function getSafeProblemMessage(error: unknown) {
  if (error instanceof ApiError) {
    if (error.status === 429) {
      return 'Demasiados intentos. Espera un momento y vuelve a probar.'
    }

    const validationMessage = getFirstValidationMessage(error.problem)

    return validationMessage
      ?? error.problem?.detail
      ?? error.problem?.title
      ?? 'No se pudo completar la solicitud.'
  }

  return 'No se pudo completar la solicitud.'
}

function getFirstValidationMessage(problem?: ProblemDetails) {
  if (!problem?.errors) {
    return undefined
  }

  const firstKey = Object.keys(problem.errors)[0]

  if (!firstKey) {
    return undefined
  }

  return problem.errors[firstKey]?.[0]
}
