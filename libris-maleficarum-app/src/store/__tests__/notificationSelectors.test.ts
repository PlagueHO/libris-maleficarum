import { describe, it, expect, beforeEach } from 'vitest';
import { selectUnreadCount, selectVisibleOperations } from '../notificationSelectors';
import type { RootState } from '../store';
import type { DeleteOperationDto } from '@/services/types/asyncOperations';

describe('notification selectors', () => {
  let mockState: RootState;
  let mockOperations: DeleteOperationDto[];

  beforeEach(() => {
    mockOperations = [
      {
        id: 'op-1',
        worldId: 'test-world-id',
        rootEntityId: 'entity-1',
        rootEntityName: 'Test World',
        status: 'pending',
        totalEntities: 0,
        deletedCount: 0,
        failedCount: 0,
        failedEntityIds: null,
        errorDetails: null,
        cascade: true,
        createdBy: 'test-user',
        createdAt: '2026-02-03T08:00:00Z',
        startedAt: null,
        completedAt: null,
      },
      {
        id: 'op-2',
        worldId: 'test-world-id',
        rootEntityId: 'entity-2',
        rootEntityName: 'Old World',
        status: 'completed',
        totalEntities: 50,
        deletedCount: 50,
        failedCount: 0,
        failedEntityIds: null,
        errorDetails: null,
        cascade: true,
        createdBy: 'test-user',
        createdAt: '2026-02-03T07:00:00Z',
        startedAt: '2026-02-03T07:00:00Z',
        completedAt: '2026-02-03T07:05:00Z',
      },
      {
        id: 'op-3',
        worldId: 'test-world-id',
        rootEntityId: 'entity-3',
        rootEntityName: 'Dismissed World',
        status: 'in_progress',
        totalEntities: 267,
        deletedCount: 120,
        failedCount: 0,
        failedEntityIds: null,
        errorDetails: null,
        cascade: true,
        createdBy: 'test-user',
        createdAt: '2026-02-03T08:10:00Z',
        startedAt: '2026-02-03T08:10:00Z',
        completedAt: null,
      },
    ];

    mockState = {
      sidePanel: {
        isExpanded: true,
      },
      worldSidebar: {
        selectedWorldId: 'test-world-id',
        selectedEntityId: null,
        expandedNodeIds: [],
        mainPanelMode: 'empty',
        isWorldFormOpen: false,
        editingWorldId: null,
        editingEntityId: null,
        newEntityParentId: null,
        hasUnsavedChanges: false,
        deletingEntityId: null,
        showDeleteConfirmation: false,
        movingEntityId: null,
        creatingEntityParentId: null,
      },
      notifications: {
        sidebarOpen: false,
        metadata: {
          'op-1': { operationId: 'op-1', isRead: false, isDismissed: false, lastInteractionTimestamp: Date.now() },
          'op-2': { operationId: 'op-2', isRead: true, isDismissed: false, lastInteractionTimestamp: Date.now() },
          'op-3': { operationId: 'op-3', isRead: false, isDismissed: true, lastInteractionTimestamp: Date.now() },
        },
        lastCleanupTimestamp: Date.now(),
        pollingEnabled: true,
      },
      api: {
        queries: {
          'getDeleteOperations({"worldId":"test-world-id"})': {
            status: 'fulfilled',
            endpointName: 'getDeleteOperations',
            requestId: 'test-request-id',
            data: mockOperations,
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
    } as unknown as RootState;
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
      const emptyState: RootState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {
            'getDeleteOperations({"worldId":"test-world-id"})': {
              status: 'fulfilled',
              endpointName: 'getDeleteOperations',
              requestId: 'test-request-id',
              data: [],
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
        },
      } as unknown as RootState;

      const count = selectUnreadCount(emptyState);
      expect(count).toBe(0);
    });

    it('should return 0 when operations data is not loaded', () => {
      const noDataState: RootState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {},
        },
      } as unknown as RootState;

      const count = selectUnreadCount(noDataState);
      expect(count).toBe(0);
    });

    it('should count new operations with no metadata as unread', () => {
      const newOperations = [
        ...mockOperations,
        {
          id: 'op-4',
          worldId: 'test-world-id',
          rootEntityId: 'entity-4',
          rootEntityName: 'New World',
          status: 'pending',
          totalEntities: 0,
          deletedCount: 0,
          failedCount: 0,
          failedEntityIds: null,
          errorDetails: null,
          cascade: true,
          createdBy: 'test-user',
          createdAt: '2026-02-03T08:20:00Z',
          startedAt: null,
          completedAt: null,
        } as DeleteOperationDto,
      ];

      const stateWithNewOp: RootState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {
            'getDeleteOperations({"worldId":"test-world-id"})': {
              status: 'fulfilled',
              endpointName: 'getDeleteOperations',
              requestId: 'test-request-id',
              data: newOperations,
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
        },
      } as unknown as RootState;

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
      const emptyState: RootState = {
        ...mockState,
        api: {
          ...mockState.api,
          queries: {
            'getDeleteOperations({"worldId":"test-world-id"})': {
              status: 'fulfilled',
              endpointName: 'getDeleteOperations',
              requestId: 'test-request-id',
              data: [],
              startedTimeStamp: Date.now(),
              fulfilledTimeStamp: Date.now(),
            },
          },
        },
      } as unknown as RootState;

      const visible = selectVisibleOperations(emptyState);
      expect(visible).toEqual([]);
    });

    it('should return all operations when none are dismissed', () => {
      const noDismissedState = {
        ...mockState,
        notifications: {
          ...mockState.notifications,
          metadata: {
            'op-1': { operationId: 'op-1', isRead: false, isDismissed: false, lastInteractionTimestamp: Date.now() },
            'op-2': { operationId: 'op-2', isRead: true, isDismissed: false, lastInteractionTimestamp: Date.now() },
            'op-3': { operationId: 'op-3', isRead: false, isDismissed: false, lastInteractionTimestamp: Date.now() },
          },
        },
      } as RootState;

      const visible = selectVisibleOperations(noDismissedState);
      expect(visible).toHaveLength(3);
    });
  });
});
