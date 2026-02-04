import { describe, it, expect, beforeEach } from 'vitest';
import { selectUnreadCount, selectVisibleOperations } from '../notificationSelectors';
import type { RootState } from '../store';
import type { AsyncOperation } from '@/services/types/asyncOperations';

describe('notification selectors', () => {
  let mockState: Partial<RootState>;
  let mockOperations: AsyncOperation[];

  beforeEach(() => {
    mockOperations = [
      {
        id: 'op-1',
        type: 'DELETE',
        targetEntityId: 'entity-1',
        targetEntityName: 'Test World',
        targetEntityType: 'World',
        status: 'pending',
        progress: null,
        result: null,
        startTimestamp: '2026-02-03T08:00:00Z',
        completionTimestamp: null,
      },
      {
        id: 'op-2',
        type: 'DELETE',
        targetEntityId: 'entity-2',
        targetEntityName: 'Old World',
        targetEntityType: 'World',
        status: 'completed',
        progress: { percentComplete: 100, itemsProcessed: 50, itemsTotal: 50 },
        result: {
          success: true,
          affectedCount: 50,
          retryCount: 0,
        },
        startTimestamp: '2026-02-03T07:00:00Z',
        completionTimestamp: '2026-02-03T07:05:00Z',
      },
      {
        id: 'op-3',
        type: 'DELETE',
        targetEntityId: 'entity-3',
        targetEntityName: 'Dismissed World',
        targetEntityType: 'World',
        status: 'in-progress',
        progress: { percentComplete: 45, itemsProcessed: 120, itemsTotal: 267 },
        result: null,
        startTimestamp: '2026-02-03T08:10:00Z',
        completionTimestamp: null,
      },
    ];

    mockState = {
      notifications: {
        sidebarOpen: false,
        metadata: {
          'op-1': { read: false, dismissed: false, lastInteractionTimestamp: Date.now() },
          'op-2': { read: true, dismissed: false, lastInteractionTimestamp: Date.now() },
          'op-3': { read: false, dismissed: true, lastInteractionTimestamp: Date.now() },
        },
        lastCleanupTimestamp: Date.now(),
        pollingEnabled: true,
      },
      api: {
        queries: {
          'getAsyncOperations(undefined)': {
            status: 'fulfilled',
            endpointName: 'getAsyncOperations',
            requestId: 'test-request-id',
            data: {
              operations: mockOperations,
              totalCount: 3,
            },
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
    } as Partial<RootState>;
  });

  describe('selectUnreadCount', () => {
    it('should count operations where isRead is false and not dismissed', () => {
      const count = selectUnreadCount(mockState as RootState);

      // op-1: read=false, dismissed=false ✓ (counts)
      // op-2: read=true, dismissed=false ✗ (doesn't count)
      // op-3: read=false, dismissed=true ✗ (doesn't count - dismissed)
      expect(count).toBe(1);
    });

    it('should return 0 when no operations exist', () => {
      const emptyState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {
            'getAsyncOperations(undefined)': {
              status: 'fulfilled',
              endpointName: 'getAsyncOperations',
              requestId: 'test-request-id',
              data: {
                operations: [],
                totalCount: 0,
              },
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
        },
      } as RootState;

      const count = selectUnreadCount(emptyState);
      expect(count).toBe(0);
    });

    it('should return 0 when operations data is not loaded', () => {
      const noDataState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {},
        },
      } as RootState;

      const count = selectUnreadCount(noDataState);
      expect(count).toBe(0);
    });

    it('should count new operations with no metadata as unread', () => {
      const newOperations = [
        ...mockOperations,
        {
          id: 'op-4',
          type: 'DELETE',
          targetEntityId: 'entity-4',
          targetEntityName: 'New World',
          targetEntityType: 'World',
          status: 'pending',
          progress: null,
          result: null,
          startTimestamp: '2026-02-03T08:20:00Z',
          completionTimestamp: null,
        } as AsyncOperation,
      ];

      const stateWithNewOp = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {
            'getAsyncOperations(undefined)': {
              status: 'fulfilled',
              endpointName: 'getAsyncOperations',
              requestId: 'test-request-id',
              data: {
                operations: newOperations,
                totalCount: 4,
              },
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
        },
      } as RootState;

      const count = selectUnreadCount(stateWithNewOp);

      // op-1: read=false, dismissed=false ✓
      // op-2: read=true, dismissed=false ✗
      // op-3: read=false, dismissed=true ✗
      // op-4: no metadata (read=false, dismissed=false) ✓
      expect(count).toBe(2);
    });
  });

  describe('selectVisibleOperations', () => {
    it('should exclude dismissed operations', () => {
      const visible = selectVisibleOperations(mockState as RootState);

      expect(visible).toHaveLength(2);
      expect(visible.find((op) => op.id === 'op-1')).toBeDefined();
      expect(visible.find((op) => op.id === 'op-2')).toBeDefined();
      expect(visible.find((op) => op.id === 'op-3')).toBeUndefined(); // Dismissed
    });

    it('should sort operations by startTimestamp descending (most recent first)', () => {
      const visible = selectVisibleOperations(mockState as RootState);

      // Expected order: op-1 (08:00), op-2 (07:00)
      // Most recent first: op-1 then op-2
      expect(visible[0].id).toBe('op-1'); // 08:00
      expect(visible[1].id).toBe('op-2'); // 07:00
    });

    it('should return empty array when no operations exist', () => {
      const emptyState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {
            'getAsyncOperations(undefined)': {
              status: 'fulfilled',
              endpointName: 'getAsyncOperations',
              requestId: 'test-request-id',
              data: {
                operations: [],
                totalCount: 0,
              },
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
        },
      } as RootState;

      const visible = selectVisibleOperations(emptyState);
      expect(visible).toEqual([]);
    });

    it('should return all operations when none are dismissed', () => {
      const noDismissedState = {
        ...mockState,
        notifications: {
          ...mockState.notifications,
          metadata: {
            'op-1': { read: false, dismissed: false, lastInteractionTimestamp: Date.now() },
            'op-2': { read: true, dismissed: false, lastInteractionTimestamp: Date.now() },
            'op-3': { read: false, dismissed: false, lastInteractionTimestamp: Date.now() },
          },
        },
      } as RootState;

      const visible = selectVisibleOperations(noDismissedState);
      expect(visible).toHaveLength(3);
    });
  });
});
