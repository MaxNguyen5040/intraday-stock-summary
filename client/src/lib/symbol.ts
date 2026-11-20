// Mirrors StockSymbol on the server so obvious typos never leave the browser.
const SYMBOL_PATTERN = /^\^?[A-Z0-9][A-Z0-9.\-=]*$/
export const MAX_SYMBOL_LENGTH = 15

export function normalizeSymbol(input: string): string | null {
  const symbol = input.trim().toUpperCase()
  if (symbol.length === 0 || symbol.length > MAX_SYMBOL_LENGTH || !SYMBOL_PATTERN.test(symbol)) {
    return null
  }
  return symbol
}
