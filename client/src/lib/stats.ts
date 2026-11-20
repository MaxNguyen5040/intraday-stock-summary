import type { DailySummary } from '../api/dailySummary'

export interface PeriodStats {
  firstDay: string
  lastDay: string
  tradingDays: number
  periodLow: number
  periodHigh: number
  lastMidpoint: number
  /** Change in daily midpoint from the first day to the last, as a ratio (0.05 = +5%). */
  midpointChange: number
  /** False for instruments Yahoo reports no volume for, such as FX pairs. */
  hasVolume: boolean
  averageVolume: number
  busiestDay: DailySummary
}

export const midpoint = (day: DailySummary) => (day.lowAverage + day.highAverage) / 2

/** Expects days sorted oldest first, as the API returns them. */
export function computePeriodStats(days: readonly DailySummary[]): PeriodStats | null {
  const first = days[0]
  const last = days[days.length - 1]
  if (!first || !last) {
    return null
  }

  const firstMidpoint = midpoint(first)
  const lastMidpoint = midpoint(last)

  return {
    firstDay: first.day,
    lastDay: last.day,
    tradingDays: days.length,
    periodLow: Math.min(...days.map((d) => d.lowAverage)),
    periodHigh: Math.max(...days.map((d) => d.highAverage)),
    lastMidpoint,
    midpointChange: firstMidpoint === 0 ? 0 : (lastMidpoint - firstMidpoint) / firstMidpoint,
    hasVolume: days.some((d) => d.volume > 0),
    averageVolume: days.reduce((sum, d) => sum + d.volume, 0) / days.length,
    busiestDay: days.reduce((busiest, d) => (d.volume > busiest.volume ? d : busiest)),
  }
}
