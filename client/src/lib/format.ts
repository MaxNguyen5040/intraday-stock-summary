const price = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 4,
  maximumFractionDigits: 4,
})
const shortPrice = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})
const integer = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 })
const compact = new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 })
const percent = new Intl.NumberFormat('en-US', {
  style: 'percent',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  signDisplay: 'exceptZero',
})

// Trading days are calendar dates, not instants. Formatting in UTC keeps "2026-10-20" from
// rendering as Oct 19 for anyone west of Greenwich.
const shortDate = new Intl.DateTimeFormat('en-US', {
  month: 'short',
  day: 'numeric',
  timeZone: 'UTC',
})
const longDate = new Intl.DateTimeFormat('en-US', {
  weekday: 'short',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  timeZone: 'UTC',
})

const toUtcDate = (isoDay: string) => new Date(`${isoDay}T00:00:00Z`)

export const formatPrice = (value: number) => price.format(value)
export const formatShortPrice = (value: number) => shortPrice.format(value)
export const formatVolume = (value: number) => integer.format(value)
export const formatCompactVolume = (value: number) => compact.format(value)
export const formatPercent = (ratio: number) => percent.format(ratio)
export const formatShortDate = (isoDay: string) => shortDate.format(toUtcDate(isoDay))
export const formatLongDate = (isoDay: string) => longDate.format(toUtcDate(isoDay))
