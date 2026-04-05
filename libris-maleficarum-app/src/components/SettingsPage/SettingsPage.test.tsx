import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { SettingsPanel } from './SettingsPage';

expect.extend(toHaveNoViolations);

function renderSettingsPanel(open = true) {
  const onOpenChange = vi.fn();
  return {
    onOpenChange,
    ...render(
      <SettingsPanel open={open} onOpenChange={onOpenChange} />
    ),
  };
}

describe('SettingsPanel', () => {
  it('renders the settings heading when open', () => {
    renderSettingsPanel();
    expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument();
  });

  it('renders the theme toggle when open', () => {
    renderSettingsPanel();
    expect(screen.getByRole('button', { name: /switch to/i })).toBeInTheDocument();
  });

  it('does not render content when closed', () => {
    renderSettingsPanel(false);
    expect(screen.queryByRole('heading', { name: /settings/i })).not.toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderSettingsPanel();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
