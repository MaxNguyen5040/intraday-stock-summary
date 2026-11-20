import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { sampleDays } from '../test/server'
import { DailyTable } from './DailyTable'

function rowHeaders() {
  return within(screen.getByRole('table'))
    .getAllByRole('rowheader')
    .map((cell) => cell.textContent)
}

describe('DailyTable', () => {
  it('lists the newest day first by default', () => {
    render(<DailyTable days={sampleDays} />)

    expect(rowHeaders()).toEqual(['Thu, Nov 19, 2026', 'Wed, Nov 18, 2026', 'Tue, Nov 17, 2026'])
    expect(screen.getByRole('columnheader', { name: /day/i })).toHaveAttribute(
      'aria-sort',
      'descending',
    )
  })

  it('shows prices at four decimals and the high-low spread', () => {
    render(<DailyTable days={sampleDays} />)

    const row = screen.getByRole('row', { name: /Nov 17/ })
    expect(
      within(row)
        .getAllByRole('cell')
        .map((c) => c.textContent),
    ).toEqual(['100.1234', '102.5000', '2.3766', '1,000,000'])
  })

  it('sorts by a column and toggles direction', async () => {
    const user = userEvent.setup()
    render(<DailyTable days={sampleDays} />)

    await user.click(screen.getByRole('button', { name: /volume/i }))
    expect(rowHeaders()).toEqual(['Tue, Nov 17, 2026', 'Thu, Nov 19, 2026', 'Wed, Nov 18, 2026'])
    expect(screen.getByRole('columnheader', { name: /volume/i })).toHaveAttribute(
      'aria-sort',
      'ascending',
    )

    await user.click(screen.getByRole('button', { name: /volume/i }))
    expect(rowHeaders()[0]).toBe('Wed, Nov 18, 2026')
  })
})
