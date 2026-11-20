import { http, HttpResponse } from 'msw/http'
import { describe, expect, it } from 'vitest'
import { problem, sampleDays, server, SUMMARY_URL } from '../test/server'
import { ApiError, fetchDailySummary, type ApiErrorKind } from './dailySummary'

async function captureError(promise: Promise<unknown>): Promise<ApiError> {
  const error = await promise.then(
    () => {
      throw new Error('Expected the request to fail')
    },
    (e: unknown) => e,
  )
  expect(error).toBeInstanceOf(ApiError)
  return error as ApiError
}

describe('fetchDailySummary', () => {
  it('returns the parsed daily summaries', async () => {
    await expect(fetchDailySummary('TSLA')).resolves.toEqual(sampleDays)
  })

  it('url-encodes the symbol', async () => {
    let requested = ''
    server.use(
      http.get(SUMMARY_URL, ({ request }) => {
        requested = new URL(request.url).pathname
        return HttpResponse.json([])
      }),
    )

    await fetchDailySummary('^GSPC')

    expect(requested).toBe('/api/v1/stocks/%5EGSPC/daily-summary')
  })

  it.each<[number, ApiErrorKind]>([
    [400, 'invalid-symbol'],
    [404, 'not-found'],
    [429, 'rate-limited'],
    [503, 'rate-limited'],
    [502, 'upstream'],
    [500, 'unexpected'],
  ])('maps HTTP %i to %s', async (status, kind) => {
    server.use(http.get(SUMMARY_URL, () => problem(status, 'Failed', 'Problem detail text')))

    const error = await captureError(fetchDailySummary('TSLA'))

    expect(error.kind).toBe(kind)
    expect(error.status).toBe(status)
    expect(error.message).toBe('Problem detail text')
  })

  it('reports network failures', async () => {
    server.use(http.get(SUMMARY_URL, () => HttpResponse.error()))

    const error = await captureError(fetchDailySummary('TSLA'))

    expect(error.kind).toBe('network')
    expect(error.isTransient).toBe(true)
  })

  it('rejects a response that does not match the contract', async () => {
    server.use(http.get(SUMMARY_URL, () => HttpResponse.json([{ day: '2026-11-19', low: 1 }])))

    const error = await captureError(fetchDailySummary('TSLA'))

    expect(error.kind).toBe('unexpected')
  })
})
