import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SymbolSearch } from './SymbolSearch'

function setup(initialValue?: string) {
  const user = userEvent.setup()
  render(<SymbolSearch initialValue={initialValue} onSearch={vi.fn()} />)
  return { user, input: screen.getByLabelText(/ticker symbol/i) as HTMLInputElement }
}

describe('SymbolSearch / shortcut', () => {
  it('focuses the input from the page without typing the slash', async () => {
    const { user, input } = setup()
    expect(document.body).toHaveFocus()

    await user.keyboard('/')

    expect(input).toHaveFocus()
    expect(input).toHaveValue('')
  })

  it('selects the current symbol when pressed from a quick pick', async () => {
    const { user, input } = setup('TSLA')
    screen.getByRole('button', { name: 'AAPL' }).focus()

    await user.keyboard('/')

    expect(input).toHaveFocus()
    expect(input.selectionStart).toBe(0)
    expect(input.selectionEnd).toBe(4)
  })

  it('lets a slash be typed while the input has focus', async () => {
    const { user, input } = setup()

    await user.type(input, 'a/b')

    expect(input).toHaveValue('a/b')
  })

  it('ignores the slash when a modifier is held', async () => {
    const { user, input } = setup()

    await user.keyboard('{Control>}/{/Control}')

    expect(input).not.toHaveFocus()
    expect(document.body).toHaveFocus()
  })
})
