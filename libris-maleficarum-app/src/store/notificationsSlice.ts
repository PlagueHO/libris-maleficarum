/**
 * Delete Operations Notifications Redux Slice
 *
 * Manages client-side state for delete operation notifications in the notification center.
 * Tracks client-side metadata (read/dismissed status) separate from server-side
 * operation data (fetched via RTK Query).
 *
 * @module notificationsSlice
 */

import { createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { RootState } from './store';
import type { DeleteOperationMetadata, DeleteOperationsState } from '@/services/types/asyncOperations';

/**
 * Initial state
 */
const initialState: DeleteOperationsState = {
  sidebarOpen: false,
  metadata: {},
  lastCleanupTimestamp: Date.now(),
  pollingEnabled: true,
};

/**
 * Delete Operations Notifications slice
 */
const notificationsSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    /**
     * Toggle notification center sidebar open/closed
     */
    toggleSidebar: (state) => {
      state.sidebarOpen = !state.sidebarOpen;
    },

    /**
     * Explicitly set sidebar open state
     */
    setSidebarOpen: (state, action: PayloadAction<boolean>) => {
      state.sidebarOpen = action.payload;
    },

    /**
     * Mark a notification as read
     */
    markAsRead: (state, action: PayloadAction<string>) => {
      const operationId = action.payload;
      if (!state.metadata[operationId]) {
        state.metadata[operationId] = {
          operationId,
          isRead: true,
          isDismissed: false,
          lastInteractionTimestamp: Date.now(),
        };
      } else {
        state.metadata[operationId].isRead = true;
        state.metadata[operationId].lastInteractionTimestamp = Date.now();
      }
    },

    /**
     * Mark all notifications as read
     */
    markAllAsRead: (state, action: PayloadAction<string[]>) => {
      const operationIds = action.payload;
      operationIds.forEach((id) => {
        if (!state.metadata[id]) {
          state.metadata[id] = {
            operationId: id,
            isRead: true,
            isDismissed: false,
            lastInteractionTimestamp: Date.now(),
          };
        } else {
          state.metadata[id].isRead = true;
          state.metadata[id].lastInteractionTimestamp = Date.now();
        }
      });
    },

    /**
     * Dismiss a notification (hide from notification center)
     */
    dismissNotification: (state, action: PayloadAction<string>) => {
      const operationId = action.payload;
      if (!state.metadata[operationId]) {
        state.metadata[operationId] = {
          operationId,
          isRead: true,
          isDismissed: true,
          lastInteractionTimestamp: Date.now(),
        };
      } else {
        state.metadata[operationId].isDismissed = true;
        state.metadata[operationId].isRead = true; // Dismissed implies read
        state.metadata[operationId].lastInteractionTimestamp = Date.now();
      }
    },

    /**
     * Clear all completed notifications (dismiss multiple)
     */
    clearAllCompleted: (state, action: PayloadAction<string[]>) => {
      const completedOperationIds = action.payload;
      completedOperationIds.forEach((id) => {
        if (!state.metadata[id]) {
          state.metadata[id] = {
            operationId: id,
            isRead: true,
            isDismissed: true,
            lastInteractionTimestamp: Date.now(),
          };
        } else {
          state.metadata[id].isDismissed = true;
          state.metadata[id].isRead = true;
          state.metadata[id].lastInteractionTimestamp = Date.now();
        }
      });
    },

    /**
     * Perform 24-hour cleanup (remove old metadata)
     */
    performCleanup: (state, action: PayloadAction<number>) => {
      const cutoffTimestamp = action.payload; // Unix timestamp 24 hours ago
      const now = Date.now();

      // Remove metadata for operations older than cutoff
      Object.keys(state.metadata).forEach((operationId) => {
        const meta = state.metadata[operationId];
        if (meta.lastInteractionTimestamp < cutoffTimestamp) {
          delete state.metadata[operationId];
        }
      });

      state.lastCleanupTimestamp = now;
    },

    /**
     * Enable/disable polling (e.g., when browser tab inactive)
     */
    setPollingEnabled: (state, action: PayloadAction<boolean>) => {
      state.pollingEnabled = action.payload;
    },
  },
});

// Export actions
export const {
  toggleSidebar,
  setSidebarOpen,
  markAsRead,
  markAllAsRead,
  dismissNotification,
  clearAllCompleted,
  performCleanup,
  setPollingEnabled,
} = notificationsSlice.actions;

// Export reducer as default
export default notificationsSlice.reducer;

// Basic selectors
export const selectSidebarOpen = (state: RootState): boolean =>
  state.notifications.sidebarOpen;

export const selectNotificationMetadata = (
  state: RootState
): Record<string, DeleteOperationMetadata> => state.notifications.metadata;

export const selectOperationMetadata =
  (operationId: string) =>
  (state: RootState): DeleteOperationMetadata | undefined =>
    state.notifications.metadata[operationId];

export const selectPollingEnabled = (state: RootState): boolean =>
  state.notifications.pollingEnabled;
