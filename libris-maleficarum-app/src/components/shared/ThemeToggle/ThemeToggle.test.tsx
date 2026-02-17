import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { ThemeToggle } from './ThemeToggle';

expect.extend(toHaveNoViolations);

vi.mock('@/hooks/useTheme', () => ({
  useTheme: vi.fn(),
}));

import { useTheme } from '@/hooks/useTheme';

const mockUseTheme = vi.mocked(useTheme);

describe('ThemeToggle', () => {
  const mockToggle = vi.fn();

  beforeEach(() => {
    mockToggle.mockClear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders Sun icon and "Switch to light mode" label in dark mode', () => {
    mockUseTheme.mockReturnValue({ theme: 'dark', toggleTheme: mockToggle });
    render(<ThemeToggle />);
    const button = screen.getByRole('button', { name: /switch to light mode/i });
    expect(button).toBeInTheDocument();
  });

  it('renders Moon icon and "Switch to dark mode" label in light mode', () => {
    mockUseTheme.mockReturnValue({ theme: 'light', toggleTheme: mockToggle });
    render(<ThemeToggle />);
    const button = screen.getByRole('button', { name: /switch to dark mode/i });
    expect(button).toBeInTheDocument();
  });

  it('calls toggleTheme on click', async () => {
    mockUseTheme.mockReturnValue({ theme: 'light', toggleTheme: mockToggle });
    const user = userEvent.setup();
    render(<ThemeToggle />);
    const button = screen.getByRole('button', { name: /switch to dark mode/i });
    await user.click(button);
    expect(mockToggle).toHaveBeenCalledOnce();
  });

  it('calls toggleTheme on Enter key', async () => {
    mockUseTheme.mockReturnValue({ theme: 'light', toggleTheme: mockToggle });
    const user = userEvent.setup();
    render(<ThemeToggle />);
    const button = screen.getByRole('button', { name: /switch to dark mode/i });
    button.focus();
    await user.keyboard('{Enter}');
    expect(mockToggle).toHaveBeenCalledOnce();
  });

  it('calls toggleTheme on Space key', async () => {
    mockUseTheme.mockReturnValue({ theme: 'light', toggleTheme: mockToggle });
    const user = userEvent.setup();
    render(<ThemeToggle />);
    const button = screen.getByRole('button', { name: /switch to dark mode/i });
    button.focus();
    await user.keyboard(' ');
    expect(mockToggle).toHaveBeenCalledOnce();
  });

  it('has no accessibility violations in light mode', async () => {
    mockUseTheme.mockReturnValue({ theme: 'light', toggleTheme: mockToggle });
    const { container } = render(<ThemeToggle />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('has no accessibility violations in dark mode', async () => {
    mockUseTheme.mockReturnValue({ theme: 'dark', toggleTheme: mockToggle });
    const { container } = render(<ThemeToggle />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
