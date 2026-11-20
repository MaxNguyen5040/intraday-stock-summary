import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { MAX_SYMBOL_LENGTH, normalizeSymbol } from '../lib/symbol'

const QUICK_PICKS = [
  { symbol: 'TSLA', description: 'Tesla, Nasdaq' },
  { symbol: 'AAPL', description: 'Apple, Nasdaq' },
  { symbol: 'MSFT', description: 'Microsoft, Nasdaq' },
  { symbol: '7203.T', description: 'Toyota, Tokyo Stock Exchange' },
  { symbol: '^GSPC', description: 'S&P 500 index' },
]

function isEditable(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) return false
  return (
    target.isContentEditable ||
    target instanceof HTMLInputElement ||
    target instanceof HTMLTextAreaElement ||
    target instanceof HTMLSelectElement
  )
}

interface SymbolSearchProps {
  initialValue?: string
  isLoading?: boolean
  onSearch: (symbol: string) => void
}

export function SymbolSearch({
  initialValue = '',
  isLoading = false,
  onSearch,
}: SymbolSearchProps) {
  const [value, setValue] = useState(initialValue)
  const [error, setError] = useState<string | null>(null)
  const inputId = useId()
  const errorId = useId()
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== '/' || event.ctrlKey || event.metaKey || event.altKey) return
      if (event.defaultPrevented || isEditable(event.target)) return
      event.preventDefault()
      inputRef.current?.focus()
      inputRef.current?.select()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const symbol = normalizeSymbol(value)
    if (!symbol) {
      setError(
        value.trim()
          ? `Use up to ${MAX_SYMBOL_LENGTH} letters, digits, ".", "-" or "=", e.g. TSLA or BRK-B.`
          : 'Enter a ticker symbol.',
      )
      return
    }
    setError(null)
    setValue(symbol)
    onSearch(symbol)
  }

  function pick(symbol: string) {
    setError(null)
    setValue(symbol)
    onSearch(symbol)
  }

  return (
    <section className="search" aria-label="Symbol search">
      <form className="search-form" onSubmit={handleSubmit} noValidate>
        <label htmlFor={inputId} className="search-label">
          Ticker symbol
          <kbd className="search-shortcut" aria-hidden="true" title="Press / to focus">
            /
          </kbd>
        </label>
        <div className="search-row">
          <input
            ref={inputRef}
            id={inputId}
            className="search-input"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            placeholder="e.g. TSLA"
            autoComplete="off"
            autoCapitalize="characters"
            spellCheck={false}
            maxLength={MAX_SYMBOL_LENGTH + 4}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            aria-keyshortcuts="/"
          />
          <button type="submit" className="button button-primary" disabled={isLoading}>
            {isLoading ? 'Loading' : 'Look up'}
          </button>
        </div>
        {error && (
          <p id={errorId} className="field-error" role="alert">
            {error}
          </p>
        )}
      </form>
      <div className="quick-picks">
        <span className="quick-picks-label">Try</span>
        {QUICK_PICKS.map(({ symbol, description }) => (
          <button
            key={symbol}
            type="button"
            className="chip"
            title={description}
            onClick={() => pick(symbol)}
          >
            {symbol}
          </button>
        ))}
      </div>
    </section>
  )
}
