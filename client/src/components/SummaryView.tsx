import { lazy, Suspense, useMemo } from 'react'
import { useDailySummary } from '../api/useDailySummary'
import { formatLongDate } from '../lib/format'
import { computePeriodStats } from '../lib/stats'
import { DailyTable } from './DailyTable'
import { EmptyPanel, ErrorPanel, LoadingPanel } from './StatusPanels'
import { SummaryStats } from './SummaryStats'

// Recharts is most of the bundle and is only needed once data has loaded.
const PriceChart = lazy(() =>
  import('./PriceChart').then((module) => ({ default: module.PriceChart })),
)

export function SummaryView({ symbol }: { symbol: string }) {
  const query = useDailySummary(symbol)
  const stats = useMemo(() => (query.data ? computePeriodStats(query.data) : null), [query.data])

  if (query.isPending) {
    return <LoadingPanel symbol={symbol} />
  }

  if (query.isError) {
    return (
      <ErrorPanel
        symbol={symbol}
        error={query.error}
        onRetry={() => void query.refetch()}
        isRetrying={query.isFetching}
      />
    )
  }

  if (!stats) {
    return <EmptyPanel symbol={symbol} />
  }

  return (
    <section className="results" aria-labelledby="results-heading">
      <header className="results-header">
        <h2 id="results-heading" className="results-symbol">
          {symbol}
        </h2>
        <p className="results-period">
          {formatLongDate(stats.firstDay)} to {formatLongDate(stats.lastDay)}
          <span aria-hidden="true"> · </span>
          {stats.tradingDays} trading days
        </p>
        <p className="results-note">The latest day may be incomplete while its market is open.</p>
      </header>
      <SummaryStats stats={stats} />
      <Suspense fallback={<div className="skeleton skeleton-chart" aria-hidden="true" />}>
        <PriceChart days={query.data} />
      </Suspense>
      <DailyTable days={query.data} />
    </section>
  )
}
