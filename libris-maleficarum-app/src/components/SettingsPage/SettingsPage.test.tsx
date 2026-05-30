import { beforeEach, describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { SettingsPage } from './SettingsPage';

const mockLoadBackendStatus = vi.fn(async () => undefined);
let mockBackendStatusState: {
  isUninitialized: boolean;
  isLoading: boolean;
  isError: boolean;
  error?: unknown;
} = {
  isUninitialized: false,
  isLoading: false,
  isError: false,
};

vi.mock('@/services/configApi', () => ({
  useLazyGetAccessStatusQuery: () => [mockLoadBackendStatus, mockBackendStatusState],
}));

expect.extend(toHaveNoViolations);

beforeEach(() => {
  vi.clearAllMocks();
  mockBackendStatusState = {
    isUninitialized: false,
    isLoading: false,
    isError: false,
  };
});

function renderSettingsPage() {
  const onClose = vi.fn();
  return {
    onClose,
    ...render(
      <SettingsPage onClose={onClose} />
    ),
  };
}

describe('SettingsPage', () => {
  it('renders heading and key settings sections', () => {
    renderSettingsPage();

    expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /appearance/i })).toBeInTheDocument();
    expect(screen.getByText(/^backend status$/i)).toBeInTheDocument();
    expect(screen.getByText(/connected/i)).toBeInTheDocument();
  });

  it('renders theme options light dark and system', async () => {
    const user = userEvent.setup();

    renderSettingsPage();

    await user.click(screen.getByRole('combobox', { name: /theme/i }));

    expect(screen.getByRole('option', { name: /light/i })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: /dark/i })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: /system/i })).toBeInTheDocument();
  });

  it('calls onClose when back button is clicked', async () => {
    const user = userEvent.setup();

    const { onClose } = renderSettingsPage();

    await user.click(screen.getByRole('button', { name: /return to world/i }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('shows disconnected state when backend check fails', async () => {
    mockBackendStatusState = {
      isUninitialized: false,
      isLoading: false,
      isError: true,
      error: { data: { detail: 'Network Error' } },
    };

    renderSettingsPage();

    expect(screen.getByText(/disconnected/i)).toBeInTheDocument();

    expect(screen.getByText(/network error/i)).toBeInTheDocument();
  });

  it('refreshes backend status when refresh is clicked', async () => {
    const user = userEvent.setup();

    renderSettingsPage();

    const initialCalls = mockLoadBackendStatus.mock.calls.length;

    await user.click(screen.getByRole('button', { name: /refresh backend status/i }));

    expect(mockLoadBackendStatus.mock.calls.length).toBeGreaterThan(initialCalls);
  });

  it('has no accessibility violations', async () => {
    const { container } = renderSettingsPage();

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
