import { useMemo, useState } from 'react'
import type { DailySummary } from '../api/dailySummary'
import { formatLongDate, formatPrice, formatVolume } from '../lib/format'

type SortKey = 'day' | 'lowAverage' | 'highAverage' | 'spread' | 'volume'
type SortDirection = 'ascending' | 'descending'

interface Column {
  key: SortKey
  label: string
  numeric: boolean
}

const COLUMNS: Column[] = [
  { key: 'day', label: 'Day', numeric: false },
  { key: 'lowAverage', label: 'Low avg', numeric: true },
  { key: 'highAverage', label: 'High avg', numeric: true },
  { key: 'spread', label: 'Spread', numeric: true },
  { key: 'volume', label: 'Volume', numeric: true },
]

const spread = (d: DailySummary) => d.highAverage - d.lowAverage

function sortValue(day: DailySummary, key: SortKey): number | string {
  return key === 'spread' ? spread(day) : day[key]
}

export function DailyTable({ days }: { days: readonly DailySummary[] }) {
  const [sort, setSort] = useState<{ key: SortKey; direction: SortDirection }>({
    key: 'day',
    direction: 'descending',
  })

  const rows = useMemo(() => {
    const factor = sort.direction === 'ascending' ? 1 : -1
    return [...days].sort((a, b) => {
      const left = sortValue(a, sort.key)
      const right = sortValue(b, sort.key)
      return (left < right ? -1 : left > right ? 1 : 0) * factor
    })
  }, [days, sort])

  function toggle(key: SortKey) {
    setSort((current) =>
      current.key === key
        ? { key, direction: current.direction === 'ascending' ? 'descending' : 'ascending' }
        : { key, direction: key === 'day' ? 'descending' : 'ascending' },
    )
  }

  return (
    <div className="table-scroll">
      <table className="daily-table">
        <caption className="visually-hidden">
          Daily averages, sortable by column. Prices are averages of 15-minute bars.
        </caption>
        <thead>
          <tr>
            {COLUMNS.map((column) => {
              const active = sort.key === column.key
              return (
                <th
                  key={column.key}
                  scope="col"
                  className={column.numeric ? 'numeric' : undefined}
                  aria-sort={active ? sort.direction : 'none'}
                >
                  <button type="button" className="sort-button" onClick={() => toggle(column.key)}>
                    {column.label}
                    <span className="sort-indicator" aria-hidden="true">
                      {active ? (sort.direction === 'ascending' ? '▲' : '▼') : ''}
                    </span>
                  </button>
                </th>
              )
            })}
          </tr>
        </thead>
        <tbody>
          {rows.map((day) => (
            <tr key={day.day}>
              <th scope="row">{formatLongDate(day.day)}</th>
              <td className="numeric">{formatPrice(day.lowAverage)}</td>
              <td className="numeric">{formatPrice(day.highAverage)}</td>
              <td className="numeric">{formatPrice(spread(day))}</td>
              <td className="numeric">{formatVolume(day.volume)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
