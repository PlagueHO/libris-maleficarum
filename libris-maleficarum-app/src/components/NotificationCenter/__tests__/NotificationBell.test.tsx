/**
 * NotificationBell Component Tests
 * 
 * Tests bell icon button with unread badge functionality
 * 
 * @module NotificationCenter/__tests__/NotificationBell
 */

import { describe,it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { axe, toHaveNoViolations } from 'jest-axe';

import { NotificationBell } from '../NotificationBell';
import notificationsReducer from '@/store/notificationsSlice';
import worldSidebarReducer from '@/store/worldSidebarSlice';
import { api } from '@/services/api';
import type { RootState } from '@/__tests__/test-utils';

expect.extend(toHaveNoViolations);

describe('NotificationBell', () => {
  function createMockStore(unreadCount = 0) {
    // Mock RTK Query state with operations
    const operations = Array.from({ length: unreadCount }, (_, i) => ({
      id: `op-${i + 1}`,
      worldId: 'test-world-id',
      rootEntityId: `entity-${i + 1}`,
      rootEntityName: `Entity ${i + 1}`,
      status: 'in_progress' as const,
      totalEntities: 0,
      deletedCount: 0,
      failedCount: 0,
      failedEntityIds: null,
      errorDetails: null,
      cascade: true,
      createdBy: 'test-user',
      createdAt: new Date().toISOString(),
      startedAt: new Date().toISOString(),
      completedAt: null,
    }));

    return configureStore({
      reducer: {
        // @ts-expect-error - Reducer type inference issues
        notifications: notificationsReducer,
        // @ts-expect-error - Reducer type inference issues
        worldSidebar: worldSidebarReducer,
        // @ts-expect-error - Reducer type inference issues with RTK Query
        [api.reducerPath]: api.reducer,
      },
      // @ts-expect-error - Middleware type inference issue with RTK Query
      middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(api.middleware),
      preloadedState: {
        notifications: {
          sidebarOpen: false,
          metadata: {},
          lastCleanupTimestamp: Date.now(),
          pollingEnabled: true,
        },
        worldSidebar: {
          rootWorldId: null,
          selectedNodeId: null,
          expandedNodeIds: [],
        },
        [api.reducerPath]: {
          queries: {
            'getDeleteOperations({"worldId":"PLACEHOLDER"})': {
              status: 'fulfilled',
              endpointName: 'getDeleteOperations',
              requestId: 'test-request-id',
              data: operations,
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
          mutations: {},
          provided: {},
          subscriptions: {},
          config: {
            reducerPath: 'api',
            keepUnusedDataFor: 60,
            refetchOnMountOrArgChange: false,
            refetchOnFocus: false,
            refetchOnReconnect: false,
            online: true,
          },
        },
      } as unknown as Partial<RootState>,
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
