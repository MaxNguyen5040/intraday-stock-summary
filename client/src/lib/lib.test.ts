import { describe, expect, it } from 'vitest'
import { sampleDays } from '../test/server'
import { formatLongDate, formatPercent, formatPrice, formatShortDate } from './format'
import { computePeriodStats } from './stats'
import { normalizeSymbol } from './symbol'

describe('normalizeSymbol', () => {
  it.each([
    ['tsla', 'TSLA'],
    ['  brk-b ', 'BRK-B'],
    ['^gspc', '^GSPC'],
    ['eurusd=x', 'EURUSD=X'],
  ])('accepts %j as %s', (input, expected) => {
    expect(normalizeSymbol(input)).toBe(expected)
  })

  it.each(['', '   ', 'TS LA', '-TSLA', 'A'.repeat(16), '<b>'])('rejects %j', (input) => {
    expect(normalizeSymbol(input)).toBeNull()
  })
})

describe('format', () => {
  it('formats prices to the API precision of four decimals', () => {
    expect(formatPrice(373.0985)).toBe('373.0985')
    expect(formatPrice(1234.5)).toBe('1,234.5000')
  })

  it('formats trading days as calendar dates regardless of local time zone', () => {
    // Would read "Oct 19" if parsed as local midnight anywhere west of UTC.
    expect(formatShortDate('2026-10-20')).toBe('Oct 20')
    expect(formatLongDate('2026-10-20')).toBe('Tue, Oct 20, 2026')
  })

  it('signs percentages', () => {
    expect(formatPercent(0.0512)).toBe('+5.12%')
    expect(formatPercent(-0.003)).toBe('-0.30%')
  })
})

describe('computePeriodStats', () => {
  it('summarizes the period', () => {
    const stats = computePeriodStats(sampleDays)

    expect(stats).toMatchObject({
      firstDay: '2026-11-17',
      lastDay: '2026-11-19',
      tradingDays: 3,
      periodLow: 100.1234,
      periodHigh: 106.5,
      lastMidpoint: 105,
      averageVolume: 6_500_000 / 3,
      busiestDay: sampleDays[1],
    })
    expect(stats?.midpointChange).toBeCloseTo((105 - 101.3117) / 101.3117, 10)
  })

  it('returns null for an empty period', () => {
    expect(computePeriodStats([])).toBeNull()
  })
})
