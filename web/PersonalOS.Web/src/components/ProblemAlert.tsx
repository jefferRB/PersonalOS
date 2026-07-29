import { getSafeProblemMessage } from '../lib/problemDetails/problemDetails'

type ProblemAlertProps = {
  error: unknown
}

export function ProblemAlert({ error }: ProblemAlertProps) {
  return (
    <div className="alert alert-error" role="alert">
      {getSafeProblemMessage(error)}
    </div>
  )
}
