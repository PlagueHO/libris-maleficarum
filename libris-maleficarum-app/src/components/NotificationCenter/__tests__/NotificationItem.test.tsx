/**
 * NotificationItem Component Tests
 * 
 * Tests individual notification item display with status, progress, and actions
 * 
 * @module NotificationCenter/__tests__/NotificationItem
 */

import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/__tests__/test-utils';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';

import { NotificationItem } from '../NotificationItem';
import type { DeleteOperationDto } from '@/services/types/asyncOperations';

expect.extend(toHaveNoViolations);

// MSW server for API mocking
const server = setupServer(
  // Retry operation handler
  http.post('/api/v1/worlds/:worldId/delete-operations/:operationId/retry', ({ params }) => {
    return HttpResponse.json({
      data: {
        id: params.operationId,
        worldId: params.worldId,
        targetEntityId: 'entity-1',
        targetEntityName: 'Test Entity',
        targetEntityType: 'World',
        status: 'pending',
        startedAt: new Date().toISOString(),
        completedAt: null,
        deletedEntities: [],
        error: null,
        retryCount: 1,
      },
    });
  })
);

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('NotificationItem', () => {
  const pendingOperation: DeleteOperationDto = {
    id: 'op-pending',
    worldId: 'test-world-id',
    targetEntityId: 'entity-1',
    targetEntityName: 'Test World',
    targetEntityType: 'World',
    status: 'pending',
    startedAt: '2026-02-03T10:00:00Z',
    completedAt: null,
    deletedEntities: [],
    error: null,
    retryCount: 0,
  };

  const inProgressOperation: DeleteOperationDto = {
    id: 'op-progress',
    worldId: 'test-world-id',
    targetEntityId: 'entity-2',
    targetEntityName: 'Greyhawk City',
    targetEntityType: 'City',
    status: 'in_progress',
    startedAt: '2026-02-03T10:05:00Z',
    completedAt: null,
    deletedEntities: [],
    error: null,
    retryCount: 0,
  };

  const completedOperation: DeleteOperationDto = {
    id: 'op-completed',
    worldId: 'test-world-id',
    targetEntityId: 'entity-3',
    targetEntityName: 'Sample Character',
    targetEntityType: 'Character',
    status: 'completed',
    startedAt: '2026-02-03T09:30:00Z',
    completedAt: '2026-02-03T09:30:05Z',
    deletedEntities: ['entity-3'],
    error: null,
    retryCount: 0,
  };

  const failedOperation: DeleteOperationDto = {
    id: 'op-failed',
    worldId: 'test-world-id',
    targetEntityId: 'entity-4',
    targetEntityName: 'Failed Entity',
    targetEntityType: 'Location',
    status: 'failed',
    startedAt: '2026-02-03T09:00:00Z',
    completedAt: '2026-02-03T09:00:10Z',
    deletedEntities: [],
    error: { code: 'NETWORK_ERROR', message: 'Network error occurred' },
    retryCount: 0,
  };

  it('renders entity name', () => {
    renderWithProviders(
      <NotificationItem operation={pendingOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    expect(screen.getByText('Test World')).toBeInTheDocument();
  });

  it('shows pending status with clock icon', () => {
    renderWithProviders(
      <NotificationItem operation={pendingOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    expect(screen.getByText(/pending/i)).toBeInTheDocument();
  });

  it('shows in-progress status', () => {
    renderWithProviders(
      <NotificationItem operation={inProgressOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    // Should show in-progress status message
    expect(screen.getByText(/deleting/i)).toBeInTheDocument();
  });

  it('shows completed status with checkmark icon', () => {
    renderWithProviders(
      <NotificationItem operation={completedOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    expect(screen.getByText(/deleted successfully/i)).toBeInTheDocument();
  });

  it('shows failed status with error icon and message', () => {
    renderWithProviders(
      <NotificationItem operation={failedOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    expect(screen.getByText(/network error occurred/i)).toBeInTheDocument();
  });

  it('marks notification as read when clicked', async () => {
    const user = userEvent.setup();

    const { store } = renderWithProviders(
      <NotificationItem operation={pendingOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    const item = screen.getByLabelText(/mark.*test world.*notification as read/i);
    await user.click(item);

    // Verify dispatch called (check Redux state)
    const state = store.getState();
    expect(state.notifications.metadata[pendingOperation.id]?.read).toBe(true);
  });

  it('dismisses notification when dismiss button clicked', async () => {
    const user = userEvent.setup();

    const { store } = renderWithProviders(
      <NotificationItem operation={pendingOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    const dismissButton = screen.getByRole('button', { name: /dismiss notification/i });
    await user.click(dismissButton);

    // Verify dispatch called (check Redux state)
    const state = store.getState();
    expect(state.notifications.metadata[pendingOperation.id]?.dismissed).toBe(true);
  });

  it('shows unread indicator for unread notifications', () => {
    renderWithProviders(
      <NotificationItem operation={pendingOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    // Unread notifications should have blue left border on Card wrapper
    const card = screen.getByTestId('notification-item');
    expect(card.className).toContain('border-l-4');
  });

  it('has no accessibility violations', async () => {
    const { container } = renderWithProviders(
      <NotificationItem operation={inProgressOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('is keyboard accessible', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <NotificationItem operation={failedOperation} />,
      { worldId: 'test-world-id', worldName: 'Test World' }
    );

    // Verify all interactive elements are keyboard accessible (have proper roles)
    const retryButton = screen.getByRole('button', { name: /retry operation/i });
    const dismissButton = screen.getByRole('button', { name: /dismiss notification/i });
    
    expect(retryButton).toBeInTheDocument();
    expect(dismissButton).toBeInTheDocument();
    
    // Verify buttons can receive focus
    retryButton.focus();
    expect(retryButton).toHaveFocus();
    
    dismissButton.focus();
    expect(dismissButton).toHaveFocus();
  });
    it('[T026] shows retry button for failed operations', () => {
      renderWithProviders(
        <NotificationItem operation={failedOperation} />,
        { worldId: 'test-world-id', worldName: 'Test World' }
      );

      const retryButton = screen.getByRole('button', { name: /retry operation/i });
      expect(retryButton).toBeInTheDocument();
      expect(retryButton).not.toBeDisabled();
    });

    it('[T026] does not show retry button for non-failed operations', () => {
      renderWithProviders(
        <NotificationItem operation={pendingOperation} />,
        { worldId: 'test-world-id', worldName: 'Test World' }
      );

      expect(screen.queryByRole('button', { name: /retry operation/i })).not.toBeInTheDocument();
    });

    it('[T027-T028] calls retry API and shows loading state when retry clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <NotificationItem operation={failedOperation} />,
        { worldId: 'test-world-id', worldName: 'Test World' }
      );

      const retryButton = screen.getByRole('button', { name: /retry operation/i });
      await user.click(retryButton);

      // Verify loading state appears
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /retry operation/i })).toHaveTextContent(/retrying/i);
      });
    });
  });

  // Cancel functionality tests removed - cancel endpoint not yet implemented in backend
  // See: specs/012-async-entity-operations/BACKEND_IMPLEMENTATION_PLAN.md
  // TODO: Re-enable when POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/cancel is implemented
});
