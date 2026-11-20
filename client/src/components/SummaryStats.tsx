import type { PeriodStats } from '../lib/stats'
import {
  formatCompactVolume,
  formatPercent,
  formatShortDate,
  formatShortPrice,
} from '../lib/format'

export function SummaryStats({ stats }: { stats: PeriodStats }) {
  const direction =
    stats.midpointChange > 0 ? 'positive' : stats.midpointChange < 0 ? 'negative' : 'neutral'

  return (
    <dl className="stats">
      <div className="stat">
        <dt>Latest midpoint</dt>
        <dd>
          <span className="stat-value">{formatShortPrice(stats.lastMidpoint)}</span>
          <span className={`stat-delta stat-delta-${direction}`}>
            {formatPercent(stats.midpointChange)} over period
          </span>
        </dd>
      </div>
      <div className="stat">
        <dt>Period range</dt>
        <dd>
          <span className="stat-value">
            {formatShortPrice(stats.periodLow)} – {formatShortPrice(stats.periodHigh)}
          </span>
          <span className="stat-note">lowest low avg to highest high avg</span>
        </dd>
      </div>
      {stats.hasVolume && (
        <>
          <div className="stat">
            <dt>Avg daily volume</dt>
            <dd>
              <span className="stat-value">{formatCompactVolume(stats.averageVolume)}</span>
              <span className="stat-note">over {stats.tradingDays} trading days</span>
            </dd>
          </div>
          <div className="stat">
            <dt>Busiest day</dt>
            <dd>
              <span className="stat-value">{formatShortDate(stats.busiestDay.day)}</span>
              <span className="stat-note">
                {formatCompactVolume(stats.busiestDay.volume)} shares
              </span>
            </dd>
          </div>
        </>
      )}
    </dl>
  )
}
