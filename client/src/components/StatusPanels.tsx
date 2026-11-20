import { ApiError } from '../api/dailySummary'

export function LoadingPanel({ symbol }: { symbol: string }) {
  return (
    <div className="panel panel-loading" role="status" aria-live="polite">
      <span className="visually-hidden">Loading {symbol}</span>
      <div className="skeleton skeleton-stats" aria-hidden="true" />
      <div className="skeleton skeleton-chart" aria-hidden="true" />
      <div className="skeleton skeleton-table" aria-hidden="true" />
    </div>
  )
}

interface ErrorPanelProps {
  symbol: string
  error: Error
  onRetry: () => void
  isRetrying: boolean
}

export function ErrorPanel({ symbol, error, onRetry, isRetrying }: ErrorPanelProps) {
  const { title, message } = describeError(symbol, error)
  const canRetry = !(error instanceof ApiError) || error.isTransient || error.kind === 'unexpected'

  return (
    <div className="panel panel-error" role="alert">
      <h2 className="panel-title">{title}</h2>
      <p>{message}</p>
      {canRetry && (
        <button type="button" className="button" onClick={onRetry} disabled={isRetrying}>
          {isRetrying ? 'Retrying' : 'Try again'}
        </button>
      )}
    </div>
  )
}

export function EmptyPanel({ symbol }: { symbol: string }) {
  return (
    <div className="panel" role="status">
      <h2 className="panel-title">No trades for {symbol}</h2>
      <p>The symbol exists, but there were no intraday trades in the last month.</p>
    </div>
  )
}

export function WelcomePanel() {
  return (
    <div className="panel panel-welcome">
      <h2 className="panel-title">Look up a symbol to get started</h2>
      <p>
        You will see each trading day from the last month, with the average of its 15-minute lows
        and highs and the total volume traded.
      </p>
    </div>
  )
}

function describeError(symbol: string, error: Error): { title: string; message: string } {
  if (!(error instanceof ApiError)) {
    return { title: 'Something went wrong', message: error.message }
  }
  switch (error.kind) {
    case 'invalid-symbol':
      return { title: 'Invalid symbol', message: error.message }
    case 'not-found':
      return {
        title: `No data for ${symbol}`,
        message: 'Check the ticker and try again. Some exchanges need a suffix, e.g. 7203.T.',
      }
    case 'rate-limited':
      return {
        title: 'Too many requests',
        message: 'The data provider is busy right now. Wait a few seconds and try again.',
      }
    case 'upstream':
      return {
        title: 'Market data unavailable',
        message: 'The market data provider is not responding. Try again shortly.',
      }
    case 'network':
      return {
        title: 'Cannot reach the server',
        message: 'Check your connection and that the API is running, then try again.',
      }
    case 'unexpected':
      return { title: 'Something went wrong', message: error.message }
  }
}
