import { useCallback, useSyncExternalStore } from 'react'
import { normalizeSymbol } from './symbol'

const PARAM = 'symbol'

function subscribe(onChange: () => void) {
  window.addEventListener('popstate', onChange)
  return () => window.removeEventListener('popstate', onChange)
}

function readSymbol(): string | null {
  return normalizeSymbol(new URLSearchParams(window.location.search).get(PARAM) ?? '')
}

/**
 * The selected symbol lives in the URL (?symbol=TSLA) so results can be bookmarked and
 * shared, and the back button moves between lookups.
 */
export function useSymbolParam(): [string | null, (symbol: string) => void] {
  const symbol = useSyncExternalStore(subscribe, readSymbol)

  const setSymbol = useCallback((next: string) => {
    const url = new URL(window.location.href)
    if (url.searchParams.get(PARAM) === next) {
      return
    }
    url.searchParams.set(PARAM, next)
    window.history.pushState(null, '', url)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, [])

  return [symbol, setSymbol]
}
