import { http, HttpResponse } from 'msw/http'
import { setupServer } from 'msw/node'
import type { DailySummary } from '../api/dailySummary'

export const SUMMARY_URL = '*/api/v1/stocks/:symbol/daily-summary'

export const sampleDays: DailySummary[] = [
  { day: '2026-11-17', lowAverage: 100.1234, highAverage: 102.5, volume: 1_000_000 },
  { day: '2026-11-18', lowAverage: 101, highAverage: 104.25, volume: 3_500_000 },
  { day: '2026-11-19', lowAverage: 103.5, highAverage: 106.5, volume: 2_000_000 },
]

export const server = setupServer(http.get(SUMMARY_URL, () => HttpResponse.json(sampleDays)))

export function problem(status: number, title: string, detail?: string) {
  return HttpResponse.json(
    { status, title, detail },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  )
}
