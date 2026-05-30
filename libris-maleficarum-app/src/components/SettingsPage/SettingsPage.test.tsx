import { beforeEach, describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { SettingsPage } from './SettingsPage';

vi.mock('@/services/configService', () => ({
  getAccessStatus: vi.fn(),
}));

import { getAccessStatus } from '@/services/configService';

expect.extend(toHaveNoViolations);

const mockGetAccessStatus = vi.mocked(getAccessStatus);

beforeEach(() => {
  vi.clearAllMocks();
  mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: false });
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
  it('renders heading and key settings sections', async () => {
    renderSettingsPage();

    expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /appearance/i })).toBeInTheDocument();
    expect(screen.getByText(/^backend status$/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText(/connected/i)).toBeInTheDocument();
    });
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
    mockGetAccessStatus.mockRejectedValue(new Error('Network Error'));

    renderSettingsPage();

    await waitFor(() => {
      expect(screen.getByText(/disconnected/i)).toBeInTheDocument();
    });

    expect(screen.getByText(/network error/i)).toBeInTheDocument();
  });

  it('refreshes backend status when refresh is clicked', async () => {
    const user = userEvent.setup();

    renderSettingsPage();

    await waitFor(() => {
      expect(screen.getByText(/connected/i)).toBeInTheDocument();
    });

    const initialCalls = mockGetAccessStatus.mock.calls.length;

    await user.click(screen.getByRole('button', { name: /refresh backend status/i }));

    await waitFor(() => {
      expect(mockGetAccessStatus.mock.calls.length).toBeGreaterThan(initialCalls);
    });
  });

  it('has no accessibility violations', async () => {
    const { container } = renderSettingsPage();

    await waitFor(() => {
      expect(screen.getByText(/connected/i)).toBeInTheDocument();
    });

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
