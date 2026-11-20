export interface DailySummary {
  /** Trading day in the exchange's time zone, `YYYY-MM-DD`. */
  day: string
  lowAverage: number
  highAverage: number
  volume: number
}

export type ApiErrorKind =
  'invalid-symbol' | 'not-found' | 'rate-limited' | 'upstream' | 'network' | 'unexpected'

export class ApiError extends Error {
  readonly kind: ApiErrorKind
  readonly status?: number

  constructor(kind: ApiErrorKind, message: string, status?: number) {
    super(message)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
  }

  /** Worth retrying automatically or offering a retry button for. */
  get isTransient(): boolean {
    return this.kind === 'rate-limited' || this.kind === 'upstream' || this.kind === 'network'
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
}

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export async function fetchDailySummary(
  symbol: string,
  signal?: AbortSignal,
): Promise<DailySummary[]> {
  const url = `${API_BASE}/api/v1/stocks/${encodeURIComponent(symbol)}/daily-summary`

  let response: Response
  try {
    response = await fetch(url, { signal, headers: { Accept: 'application/json' } })
  } catch (error) {
    if (signal?.aborted) {
      throw error
    }
    throw new ApiError('network', 'Could not reach the server.')
  }

  if (!response.ok) {
    const problem = await readProblem(response)
    // The API always answers with problem+json. A bare 502/503/504 came from a proxy or load
    // balancer in front of it, meaning our server is down, not the market data provider.
    if (!problem && GATEWAY_STATUSES.has(response.status)) {
      throw new ApiError('network', 'Could not reach the server.', response.status)
    }
    throw new ApiError(
      kindForStatus(response.status),
      problem?.detail ?? problem?.title ?? response.statusText,
      response.status,
    )
  }

  const body: unknown = await response.json().catch(() => undefined)
  if (!isDailySummaryList(body)) {
    throw new ApiError('unexpected', 'The server returned data in an unexpected format.')
  }
  return body
}

const GATEWAY_STATUSES = new Set([502, 503, 504])

function kindForStatus(status: number): ApiErrorKind {
  switch (status) {
    case 400:
      return 'invalid-symbol'
    case 404:
      return 'not-found'
    case 429:
    case 503:
      return 'rate-limited'
    case 502:
    case 504:
      return 'upstream'
    default:
      return 'unexpected'
  }
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  if (!response.headers.get('Content-Type')?.includes('json')) {
    return undefined
  }
  try {
    const body: unknown = await response.json()
    return typeof body === 'object' && body !== null ? (body as ProblemDetails) : undefined
  } catch {
    return undefined
  }
}

function isDailySummaryList(value: unknown): value is DailySummary[] {
  return (
    Array.isArray(value) &&
    value.every(
      (item: Partial<DailySummary> | null) =>
        typeof item?.day === 'string' &&
        typeof item.lowAverage === 'number' &&
        typeof item.highAverage === 'number' &&
        typeof item.volume === 'number',
    )
  )
}
