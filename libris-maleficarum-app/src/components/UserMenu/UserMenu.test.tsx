import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { UserMenu } from './UserMenu';

expect.extend(toHaveNoViolations);

function renderUserMenu() {
  const onOpenSettings = vi.fn();
  return {
    onOpenSettings,
    ...render(
      <UserMenu onOpenSettings={onOpenSettings} />
    ),
  };
}

describe('UserMenu', () => {
  it('renders the user menu trigger button', () => {
    renderUserMenu();
    expect(screen.getByRole('button', { name: /user menu/i })).toBeInTheDocument();
  });

  it('shows anonymous mode display by default', async () => {
    const user = userEvent.setup();
    renderUserMenu();

    await user.click(screen.getByRole('button', { name: /user menu/i }));

    expect(screen.getByText(/anonymous/i)).toBeInTheDocument();
  });

  it('shows settings menu item when opened', async () => {
    const user = userEvent.setup();
    renderUserMenu();

    await user.click(screen.getByRole('button', { name: /user menu/i }));

    expect(screen.getByRole('menuitem', { name: /settings/i })).toBeInTheDocument();
  });

  it('closes menu on escape key', async () => {
    const user = userEvent.setup();
    renderUserMenu();

    await user.click(screen.getByRole('button', { name: /user menu/i }));
    expect(screen.getByText(/anonymous/i)).toBeInTheDocument();

    await user.keyboard('{Escape}');
    expect(screen.queryByRole('menuitem', { name: /settings/i })).not.toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = renderUserMenu();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('has no accessibility violations when menu is open', async () => {
    const user = userEvent.setup();
    const { container } = renderUserMenu();

    await user.click(screen.getByRole('button', { name: /user menu/i }));

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
