import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  type TooltipContentProps,
} from 'recharts'
import type { DailySummary } from '../api/dailySummary'
import {
  formatCompactVolume,
  formatLongDate,
  formatPrice,
  formatShortDate,
  formatShortPrice,
  formatVolume,
} from '../lib/format'

interface ChartPoint extends DailySummary {
  range: [number, number]
}

export function PriceChart({ days }: { days: readonly DailySummary[] }) {
  const data: ChartPoint[] = days.map((d) => ({ ...d, range: [d.lowAverage, d.highAverage] }))

  return (
    <figure className="chart">
      <figcaption className="chart-caption">
        <span className="legend-swatch legend-swatch-band" aria-hidden="true" />
        Daily low avg to high avg
        <span className="legend-swatch legend-swatch-volume" aria-hidden="true" />
        Volume
      </figcaption>
      <div
        className="chart-canvas"
        role="img"
        aria-label="Chart of daily average low and high prices with volume"
      >
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--chart-grid)" />
            <XAxis
              dataKey="day"
              tickFormatter={formatShortDate}
              tick={{ fill: 'var(--text-muted)', fontSize: 12 }}
              tickLine={false}
              axisLine={{ stroke: 'var(--border)' }}
              minTickGap={24}
            />
            <YAxis
              yAxisId="price"
              orientation="left"
              domain={['auto', 'auto']}
              tickFormatter={formatShortPrice}
              tick={{ fill: 'var(--text-muted)', fontSize: 12 }}
              tickLine={false}
              axisLine={false}
              width={64}
            />
            {/* Volume is scaled to the bottom quarter so bars don't compete with the price band. */}
            <YAxis
              yAxisId="volume"
              orientation="right"
              domain={[0, (max: number) => max * 4]}
              tickFormatter={formatCompactVolume}
              tick={{ fill: 'var(--text-muted)', fontSize: 12 }}
              tickLine={false}
              axisLine={false}
              width={48}
            />
            <Tooltip content={ChartTooltip} cursor={{ fill: 'var(--chart-cursor)' }} />
            <Bar
              yAxisId="volume"
              dataKey="volume"
              fill="var(--chart-volume)"
              radius={[2, 2, 0, 0]}
              isAnimationActive={false}
            />
            <Area
              yAxisId="price"
              dataKey="range"
              type="linear"
              stroke="var(--chart-band-stroke)"
              strokeWidth={1.5}
              fill="var(--chart-band)"
              isAnimationActive={false}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </figure>
  )
}

function ChartTooltip({ active, payload }: TooltipContentProps) {
  const point = payload?.[0]?.payload as ChartPoint | undefined
  if (!active || !point) {
    return null
  }
  return (
    <div className="chart-tooltip">
      <div className="chart-tooltip-title">{formatLongDate(point.day)}</div>
      <dl>
        <dt>High avg</dt>
        <dd>{formatPrice(point.highAverage)}</dd>
        <dt>Low avg</dt>
        <dd>{formatPrice(point.lowAverage)}</dd>
        <dt>Volume</dt>
        <dd>{formatVolume(point.volume)}</dd>
      </dl>
    </div>
  )
}
