/**
 * Notification Selectors
 *
 * Derived selectors that compute notification center state by combining
 * client-side metadata (from notificationsSlice) with server-side operation
 * data (from deleteOperationsApi RTK Query cache).
 *
 * @module notificationSelectors
 */

import { createSelector } from '@reduxjs/toolkit';
import type { RootState } from './store';
import { selectNotificationMetadata } from './notificationsSlice';
import { selectSelectedWorldId } from './worldSidebarSlice';
import { deleteOperationsApi } from '@/services/asyncOperationsApi';
import type { DeleteOperationDto } from '@/services/types/asyncOperations';

/**
 * Stable empty array reference to avoid selector recomputation warnings
 */
const EMPTY_OPERATIONS: DeleteOperationDto[] = [];

/**
 * Select delete operations data from RTK Query cache
 *
 * Dynamically reads the selected worldId from the worldSidebar slice
 * and queries the matching RTK Query cache entry. Returns empty array
 * if no world is selected or no data is available.
 */
const selectDeleteOperationsData = (state: RootState): DeleteOperationDto[] => {
  const worldId = selectSelectedWorldId(state);
  if (!worldId) return EMPTY_OPERATIONS;

  const result = deleteOperationsApi.endpoints.getDeleteOperations.select({
    worldId,
  })(state);

  // Return stable empty array reference if no data
  if (!result?.data || result.data.length === 0) {
    return EMPTY_OPERATIONS;
  }

  return result.data;
};

/**
 * Compute unread count for badge
 *
 * Counts operations that are:
 * - NOT marked as read in metadata
 * - NOT dismissed
 * - Currently exist in RTK Query cache (from API)
 */
export const selectUnreadCount = createSelector(
  [selectNotificationMetadata, selectDeleteOperationsData],
  (metadata, operations): number => {
    if (!operations) return 0;

    return operations.filter((op) => {
      const meta = metadata[op.id];
      // Count if: no metadata (new operation) OR (metadata exists AND not read AND not dismissed)
      return !meta?.isRead && !meta?.isDismissed;
    }).length;
  }
);

/**
 * Get visible operations (not dismissed) sorted by recency
 *
 * Filters out dismissed operations and sorts by createdAt
 * descending (most recent first).
 */
export const selectVisibleOperations = createSelector(
  [selectNotificationMetadata, selectDeleteOperationsData],
  (metadata, operations): DeleteOperationDto[] => {
    if (!operations) return [];

    return operations
      .filter((op) => !metadata[op.id]?.isDismissed)
      .sort(
        (a, b) =>
          new Date(b.createdAt).getTime() -
          new Date(a.createdAt).getTime()
      );
  }
);

/**
 * Get active (in-progress or pending) operations
 */
export const selectActiveOperations = createSelector(
  [selectVisibleOperations],
  (operations): DeleteOperationDto[] => {
    return operations.filter(
      (op) => op.status === 'pending' || op.status === 'in_progress'
    );
  }
);

/**
 * Check if there are any pending operations
 */
export const selectHasPendingOperations = createSelector(
  [selectActiveOperations],
  (operations): boolean => operations.length > 0
);

/**
 * Calculate progress percentage for a delete operation
 * 
 * @param operation - The delete operation
 * @returns Progress percentage (0-100)
 */
export function calculateProgress(operation: DeleteOperationDto): number {
  if (operation.status === 'completed' || operation.status === 'failed') {
    return 100;
  }
  
  if (operation.status === 'partial') {
    return operation.totalEntities > 0
      ? Math.round((operation.deletedCount / operation.totalEntities) * 100)
      : 100;
  }
  
  if (operation.status === 'pending') {
    return 0;
  }
  
  // in_progress
  if (operation.totalEntities === 0) {
    return operation.deletedCount > 0 ? 50 : 0;
  }
  
  return Math.min(
    100,
    Math.round((operation.deletedCount / operation.totalEntities) * 100)
  );
}
