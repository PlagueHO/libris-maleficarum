import { describe, it, expect, beforeEach } from 'vitest';
import notificationsReducer, {
  toggleSidebar,
  setSidebarOpen,
  markAsRead,
  dismissNotification,
  clearAllCompleted,
  performCleanup,
  setPollingEnabled,
} from '../notificationsSlice';
import type { NotificationsState } from '../notificationsSlice';

describe('notificationsSlice', () => {
  let initialState: NotificationsState;

  beforeEach(() => {
    initialState = {
      sidebarOpen: false,
      metadata: {},
      lastCleanupTimestamp: Date.now(),
      pollingEnabled: true,
    };
  });

  describe('toggleSidebar', () => {
    it('should toggle sidebar from closed to open', () => {
      const state = notificationsReducer(initialState, toggleSidebar());
      expect(state.sidebarOpen).toBe(true);
    });

    it('should toggle sidebar from open to closed', () => {
      const openState = { ...initialState, sidebarOpen: true };
      const state = notificationsReducer(openState, toggleSidebar());
      expect(state.sidebarOpen).toBe(false);
    });
  });

  describe('setSidebarOpen', () => {
    it('should set sidebar to open', () => {
      const state = notificationsReducer(initialState, setSidebarOpen(true));
      expect(state.sidebarOpen).toBe(true);
    });

    it('should set sidebar to closed', () => {
      const openState = { ...initialState, sidebarOpen: true };
      const state = notificationsReducer(openState, setSidebarOpen(false));
      expect(state.sidebarOpen).toBe(false);
    });
  });

  describe('markAsRead', () => {
    it('should create new metadata when operation not tracked', () => {
      const operationId = 'op-123';
      const state = notificationsReducer(initialState, markAsRead(operationId));

      expect(state.metadata[operationId]).toBeDefined();
      expect(state.metadata[operationId].read).toBe(true);
      expect(state.metadata[operationId].dismissed).toBe(false);
      expect(state.metadata[operationId].lastInteractionTimestamp).toBeGreaterThan(0);
    });

    it('should update existing metadata to mark as read', () => {
      const operationId = 'op-123';
      const stateWithMeta: NotificationsState = {
        ...initialState,
        metadata: {
          [operationId]: {
            read: false,
            dismissed: false,
            lastInteractionTimestamp: Date.now() - 1000,
          },
        },
      };

      const state = notificationsReducer(stateWithMeta, markAsRead(operationId));

      expect(state.metadata[operationId].read).toBe(true);
      expect(state.metadata[operationId].dismissed).toBe(false);
      expect(state.metadata[operationId].lastInteractionTimestamp).toBeGreaterThan(
        stateWithMeta.metadata[operationId].lastInteractionTimestamp
      );
    });
  });

  describe('dismissNotification', () => {
    it('should create new metadata marked as dismissed and read', () => {
      const operationId = 'op-123';
      const state = notificationsReducer(initialState, dismissNotification(operationId));

      expect(state.metadata[operationId].dismissed).toBe(true);
      expect(state.metadata[operationId].read).toBe(true);
      expect(state.metadata[operationId].lastInteractionTimestamp).toBeGreaterThan(0);
    });

    it('should update existing metadata to mark as dismissed and read', () => {
      const operationId = 'op-123';
      const stateWithMeta: NotificationsState = {
        ...initialState,
        metadata: {
          [operationId]: {
            read: false,
            dismissed: false,
            lastInteractionTimestamp: Date.now() - 1000,
          },
        },
      };

      const state = notificationsReducer(stateWithMeta, dismissNotification(operationId));

      expect(state.metadata[operationId].dismissed).toBe(true);
      expect(state.metadata[operationId].read).toBe(true); // Dismissed implies read
    });
  });

  describe('clearAllCompleted', () => {
    it('should dismiss multiple operations', () => {
      const state = notificationsReducer(
        initialState,
        clearAllCompleted(['op-1', 'op-2', 'op-3'])
      );

      expect(state.metadata['op-1'].dismissed).toBe(true);
      expect(state.metadata['op-1'].read).toBe(true);
      expect(state.metadata['op-2'].dismissed).toBe(true);
      expect(state.metadata['op-2'].read).toBe(true);
      expect(state.metadata['op-3'].dismissed).toBe(true);
      expect(state.metadata['op-3'].read).toBe(true);
    });

    it('should update existing metadata for operations', () => {
      const stateWithMeta: NotificationsState = {
        ...initialState,
        metadata: {
          'op-1': { read: true, dismissed: false, lastInteractionTimestamp: Date.now() - 1000 },
          'op-2': { read: false, dismissed: false, lastInteractionTimestamp: Date.now() - 1000 },
        },
      };

      const state = notificationsReducer(stateWithMeta, clearAllCompleted(['op-1', 'op-2']));

      expect(state.metadata['op-1'].dismissed).toBe(true);
      expect(state.metadata['op-2'].dismissed).toBe(true);
      expect(state.metadata['op-2'].read).toBe(true); // Should be marked as read too
    });
  });

  describe('performCleanup', () => {
    it('should remove metadata older than cutoff timestamp', () => {
      const now = Date.now();
      const oneHourAgo = now - 60 * 60 * 1000;
      const twentyFiveHoursAgo = now - 25 * 60 * 60 * 1000;

      const stateWithOldMeta: NotificationsState = {
        ...initialState,
        metadata: {
          'op-recent': {
            read: true,
            dismissed: false,
            lastInteractionTimestamp: oneHourAgo,
          },
          'op-old': {
            read: true,
            dismissed: true,
            lastInteractionTimestamp: twentyFiveHoursAgo,
          },
        },
      };

      const cutoff = now - 24 * 60 * 60 * 1000; // 24 hours ago
      const state = notificationsReducer(stateWithOldMeta, performCleanup(cutoff));

      expect(state.metadata['op-recent']).toBeDefined(); // Recent kept
      expect(state.metadata['op-old']).toBeUndefined(); // Old removed
      expect(state.lastCleanupTimestamp).toBeGreaterThanOrEqual(now);
    });

    it('should keep all metadata when none are older than cutoff', () => {
      const now = Date.now();
      const oneHourAgo = now - 60 * 60 * 1000;

      const stateWithRecentMeta: NotificationsState = {
        ...initialState,
        metadata: {
          'op-1': { read: true, dismissed: false, lastInteractionTimestamp: oneHourAgo },
          'op-2': { read: false, dismissed: false, lastInteractionTimestamp: oneHourAgo },
        },
      };

      const cutoff = now - 24 * 60 * 60 * 1000;
      const state = notificationsReducer(stateWithRecentMeta, performCleanup(cutoff));

      expect(state.metadata['op-1']).toBeDefined();
      expect(state.metadata['op-2']).toBeDefined();
    });

    it('should update lastCleanupTimestamp', () => {
      const now = Date.now();
      const cutoff = now - 24 * 60 * 60 * 1000;

      const state = notificationsReducer(initialState, performCleanup(cutoff));

      expect(state.lastCleanupTimestamp).toBeGreaterThanOrEqual(now);
    });
  });

  describe('setPollingEnabled', () => {
    it('should enable polling', () => {
      const disabledState = { ...initialState, pollingEnabled: false };
      const state = notificationsReducer(disabledState, setPollingEnabled(true));

      expect(state.pollingEnabled).toBe(true);
    });

    it('should disable polling', () => {
      const state = notificationsReducer(initialState, setPollingEnabled(false));

      expect(state.pollingEnabled).toBe(false);
    });
  });
});
