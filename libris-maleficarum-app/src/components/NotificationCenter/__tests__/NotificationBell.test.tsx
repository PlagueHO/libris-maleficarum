/**
 * NotificationBell Component Tests
 * 
 * Tests bell icon button with unread badge functionality
 * 
 * @module NotificationCenter/__tests__/NotificationBell
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { axe, toHaveNoViolations } from 'jest-axe';

import { NotificationBell } from '../NotificationBell';
import notificationsReducer from '@/store/notificationsSlice';
import { api } from '@/services/api';

expect.extend(toHaveNoViolations);

describe('NotificationBell', () => {
  function createMockStore(unreadCount = 0) {
    // Mock RTK Query state with operations
    const operations = Array.from({ length: unreadCount }, (_, i) => ({
      id: `op-${i + 1}`,
      type: 'DELETE' as const,
      targetEntityId: `entity-${i + 1}`,
      targetEntityName: `Entity ${i + 1}`,
      targetEntityType: 'World',
      status: 'in-progress' as const,
      progress: null,
      result: null,
      startTimestamp: new Date().toISOString(),
      completionTimestamp: null,
    }));

    return configureStore({
      reducer: {
        notifications: notificationsReducer,
        [api.reducerPath]: api.reducer,
      },
      middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(api.middleware),
      preloadedState: {
        notifications: {
          sidebarOpen: false,
          metadata: {},
          lastCleanupTimestamp: Date.now(),
          pollingEnabled: true,
        },
        [api.reducerPath]: {
          queries: {
            'getAsyncOperations(undefined)': {
              status: 'fulfilled',
              data: {
                operations,
                totalCount: operations.length,
              },
            },
          },
        } as any,
      },
    });
  }

  it('renders bell icon button', () => {
    const store = createMockStore(0);
    const handleClick = vi.fn();

    render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    const button = screen.getByRole('button', { name: /notifications/i });
    expect(button).toBeInTheDocument();
  });

  it('shows unread count badge when there are unread notifications', () => {
    const store = createMockStore(2);
    const handleClick = vi.fn();

    render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByLabelText(/Notifications \(2 unread\)/i)).toBeInTheDocument();
  });

  it('does not show badge when no unread notifications', () => {
    const store = createMockStore(0);
    const handleClick = vi.fn();

    render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    expect(screen.queryByText('0')).not.toBeInTheDocument();
    expect(screen.getByLabelText(/Notifications$/i)).toBeInTheDocument();
  });

  it('shows "99+" when unread count exceeds 99', () => {
    const store = createMockStore(150);
    const handleClick = vi.fn();

    render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    expect(screen.getByText('99+')).toBeInTheDocument();
  });

  it('calls onClick when clicked', async () => {
    const user = userEvent.setup();
    const store = createMockStore(0);
    const handleClick = vi.fn();

    render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    const button = screen.getByRole('button', { name: /notifications/i });
    await user.click(button);

    expect(handleClick).toHaveBeenCalledOnce();
  });

  it('has no accessibility violations', async () => {
    const store = createMockStore(2);
    const handleClick = vi.fn();

    const { container } = render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('is keyboard accessible', async () => {
    const user = userEvent.setup();
    const store = createMockStore(0);
    const handleClick = vi.fn();

    render(
      <Provider store={store}>
        <NotificationBell onClick={handleClick} />
      </Provider>
    );

    const button = screen.getByRole('button', { name: /notifications/i });
    
    // Tab to button
    await user.tab();
    expect(button).toHaveFocus();

    // Press Enter
    await user.keyboard('{Enter}');
    expect(handleClick).toHaveBeenCalledOnce();
  });
});
