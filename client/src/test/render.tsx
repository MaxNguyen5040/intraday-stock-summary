import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactElement } from 'react'

/** Renders with a fresh QueryClient so cached results never leak between tests. */
export function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient()
  return {
    user: userEvent.setup(),
    ...render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>),
  }
}
