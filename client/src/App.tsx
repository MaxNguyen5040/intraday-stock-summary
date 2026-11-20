import { useIsFetching } from '@tanstack/react-query'
import { SummaryView } from './components/SummaryView'
import { SymbolSearch } from './components/SymbolSearch'
import { WelcomePanel } from './components/StatusPanels'
import { useSymbolParam } from './lib/useSymbolParam'

export default function App() {
  const [symbol, setSymbol] = useSymbolParam()
  const isFetching = useIsFetching() > 0

  return (
    <div className="app">
      <header className="app-header">
        <h1 className="app-title">Intraday Summary</h1>
        <p className="app-subtitle">Daily averages from 15-minute bars over the last month</p>
      </header>
      <main className="app-main">
        {/* Keyed so the input resets when the symbol changes via back/forward navigation. */}
        <SymbolSearch
          key={symbol ?? ''}
          initialValue={symbol ?? ''}
          isLoading={isFetching}
          onSearch={setSymbol}
        />
        {symbol ? <SummaryView symbol={symbol} /> : <WelcomePanel />}
      </main>
      <footer className="app-footer">Market data from Yahoo Finance. Not investment advice.</footer>
    </div>
  )
}
