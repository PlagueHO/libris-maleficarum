import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { SettingsPage } from './SettingsPage';
import { MemoryRouter } from 'react-router-dom';

expect.extend(toHaveNoViolations);

function renderSettingsPage() {
  return render(
    <MemoryRouter>
      <SettingsPage />
    </MemoryRouter>
  );
}

describe('SettingsPage', () => {
  it('renders the settings heading', () => {
    renderSettingsPage();
    expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument();
  });

  it('renders the theme toggle', () => {
    renderSettingsPage();
    expect(screen.getByRole('button', { name: /switch to/i })).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderSettingsPage();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
