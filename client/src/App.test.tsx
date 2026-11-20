import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw/http'
import { describe, expect, it } from 'vitest'
import App from './App'
import { renderWithProviders } from './test/render'
import { problem, sampleDays, server, SUMMARY_URL } from './test/server'

describe('App', () => {
  it('looks up a symbol and shows the summary, chart and table', async () => {
    const { user } = renderWithProviders(<App />)

    await user.type(screen.getByLabelText(/ticker symbol/i), 'tsla')
    await user.click(screen.getByRole('button', { name: /look up/i }))

    expect(await screen.findByRole('heading', { name: 'TSLA' })).toBeInTheDocument()
    expect(screen.getByText(/Tue, Nov 17, 2026 to Thu, Nov 19, 2026/)).toBeInTheDocument()
    expect(await screen.findByRole('img', { name: /chart/i })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: '100.1234' })).toBeInTheDocument()
    expect(window.location.search).toBe('?symbol=TSLA')
  })

  it('loads the symbol from the URL on first render', async () => {
    window.history.replaceState(null, '', '/?symbol=aapl')

    renderWithProviders(<App />)

    expect(await screen.findByRole('heading', { name: 'AAPL' })).toBeInTheDocument()
    expect(screen.getByLabelText(/ticker symbol/i)).toHaveValue('AAPL')
  })

  it('looks up a quick pick', async () => {
    const { user } = renderWithProviders(<App />)

    await user.click(screen.getByRole('button', { name: 'MSFT' }))

    expect(await screen.findByRole('heading', { name: 'MSFT' })).toBeInTheDocument()
  })

  it('validates input before calling the API', async () => {
    let calls = 0
    server.use(
      http.get(SUMMARY_URL, () => {
        calls++
        return HttpResponse.json(sampleDays)
      }),
    )
    const { user } = renderWithProviders(<App />)

    await user.type(screen.getByLabelText(/ticker symbol/i), 'not a symbol')
    await user.click(screen.getByRole('button', { name: /look up/i }))

    expect(screen.getByRole('alert')).toHaveTextContent(/letters, digits/i)
    expect(screen.getByLabelText(/ticker symbol/i)).toHaveAttribute('aria-invalid', 'true')
    expect(calls).toBe(0)
  })

  it('explains an unknown symbol without offering a pointless retry', async () => {
    server.use(http.get(SUMMARY_URL, () => problem(404, 'Symbol not found')))
    const { user } = renderWithProviders(<App />)

    await user.type(screen.getByLabelText(/ticker symbol/i), 'ZZZZ')
    await user.click(screen.getByRole('button', { name: /look up/i }))

    expect(await screen.findByRole('heading', { name: 'No data for ZZZZ' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /try again/i })).not.toBeInTheDocument()
  })

  it('recovers from an upstream failure when the user retries', async () => {
    let attempt = 0
    server.use(
      http.get(SUMMARY_URL, () =>
        ++attempt === 1
          ? problem(502, 'Market data provider error')
          : HttpResponse.json(sampleDays),
      ),
    )
    const { user } = renderWithProviders(<App />)

    await user.click(screen.getByRole('button', { name: 'TSLA' }))
    expect(
      await screen.findByRole('heading', { name: /market data unavailable/i }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /try again/i }))

    expect(await screen.findByRole('heading', { name: 'TSLA' })).toBeInTheDocument()
  })

  it('shows an empty state when the symbol has no trades', async () => {
    server.use(http.get(SUMMARY_URL, () => HttpResponse.json([])))
    const { user } = renderWithProviders(<App />)

    await user.click(screen.getByRole('button', { name: 'TSLA' }))

    expect(await screen.findByRole('heading', { name: /no trades for tsla/i })).toBeInTheDocument()
  })

  it('follows browser back navigation between lookups', async () => {
    const { user } = renderWithProviders(<App />)

    await user.click(screen.getByRole('button', { name: 'TSLA' }))
    await screen.findByRole('heading', { name: 'TSLA' })
    await user.click(screen.getByRole('button', { name: 'AAPL' }))
    await screen.findByRole('heading', { name: 'AAPL' })

    window.history.back()

    await waitFor(() => expect(screen.getByLabelText(/ticker symbol/i)).toHaveValue('TSLA'))
    expect(await screen.findByRole('heading', { name: 'TSLA' })).toBeInTheDocument()
  })
})
