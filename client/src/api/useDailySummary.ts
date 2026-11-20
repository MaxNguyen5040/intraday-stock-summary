import { useQuery } from '@tanstack/react-query'
import { ApiError, fetchDailySummary } from './dailySummary'

export function useDailySummary(symbol: string) {
  return useQuery({
    queryKey: ['daily-summary', symbol],
    queryFn: ({ signal }) => fetchDailySummary(symbol, signal),
    // Matches the API's cache; 15-minute bars don't change faster than this matters.
    staleTime: 60_000,
    retry: (failureCount, error) =>
      error instanceof ApiError && error.kind === 'network' && failureCount < 1,
  })
}
