# Data Model: Async Entity Operations with Notification Center

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)  
**Date**: February 3, 2026  
**Phase**: 1 - Data Model Design

## Overview

This document defines the TypeScript type system and Redux state shape for the async entity operations feature. All types are frontend-only (no database entities). State is session-scoped (in-memory only, no persistence across browser sessions).

## Type Definitions

### Core Entities

#### AsyncOperation

Represents a background operation being tracked by the notification center.

```typescript
/**
 * Async operation status
 */
export type AsyncOperationStatus = 'pending' | 'in-progress' | 'completed' | 'failed';

/**
 * Async operation types (extensible for future operation types)
 */
export type AsyncOperationType = 'DELETE' | 'CREATE' | 'UPDATE' | 'IMPORT' | 'EXPORT';

/**
 * Progress information for long-running operations
 */
export interface AsyncOperationProgress {
  /** Percentage complete (0-100) */
  percentComplete: number;
  
  /** Number of items processed so far */
  itemsProcessed: number;
  
  /** Total number of items to process */
  itemsTotal: number;
  
  /** Current item being processed (optional, for detailed status) */
  currentItem?: string;
}

/**
 * Result of a completed operation
 */
export interface AsyncOperationResult {
  /** Whether operation completed successfully */
  success: boolean;
  
  /** Number of entities affected (created/updated/deleted) */
  affectedCount: number;
  
  /** Error message if operation failed */
  errorMessage?: string;
  
  /** Detailed error information (for debugging/retry logic) */
  errorDetails?: {
    code: string;
    failedAtItem?: string;
    stackTrace?: string;
  };
  
  /** Number of retry attempts (if operation has been retried) */
  retryCount: number;
}

/**
 * Main async operation entity
 */
export interface AsyncOperation {
  /** Unique operation identifier (server-generated) */
  id: string;
  
  /** Type of operation being performed */
  type: AsyncOperationType;
  
  /** ID of the entity being operated on */
  targetEntityId: string;
  
  /** Name of the entity being operated on (for display) */
  targetEntityName: string;
  
  /** Type of the entity (World, Character, Location, etc.) */
  targetEntityType: string;
  
  /** Current status of the operation */
  status: AsyncOperationStatus;
  
  /** Progress information (null for pending operations) */
  progress: AsyncOperationProgress | null;
  
  /** Operation result (null until completed or failed) */
  result: AsyncOperationResult | null;
  
  /** ISO 8601 timestamp when operation started */
  startTimestamp: string;
  
  /** ISO 8601 timestamp when operation completed (null if still running) */
  completionTimestamp: string | null;
}
```

#### NotificationMetadata

Client-side metadata for managing notification UI state (read/dismissed status).

```typescript
/**
 * Client-side metadata for operation notifications
 * 
 * Stored in Redux; not persisted to backend.
 * Keys are AsyncOperation IDs.
 */
export interface NotificationMetadata {
  /** Whether user has viewed/read this notification */
  read: boolean;
  
  /** Whether user has dismissed this notification from view */
  dismissed: boolean;
  
  /** Timestamp when user last interacted with notification (for cleanup) */
  lastInteractionTimestamp: number; // Unix timestamp (ms)
}
```

### Redux State Shape

#### NotificationsSlice State

```typescript
/**
 * Redux state for notifications feature
 */
export interface NotificationsState {
  /** Whether notification center sidebar is currently open */
  sidebarOpen: boolean;
  
  /** 
   * Client-side metadata for each operation notification
   * Key: AsyncOperation ID
   * Value: Metadata (read/dismissed status)
   */
  metadata: Record<string, NotificationMetadata>;
  
  /** 
   * Unix timestamp (ms) of last cleanup run
   * Used to track 24-hour cleanup interval
   */
  lastCleanupTimestamp: number;
  
  /** 
   * Polling status (used to control polling behavior)
   */
  pollingEnabled: boolean;
}

/**
 * Initial state
 */
export const initialNotificationsState: NotificationsState = {
  sidebarOpen: false,
  metadata: {},
  lastCleanupTimestamp: Date.now(),
  pollingEnabled: true,
};
```

## Redux Slice Definition

### Actions

```typescript
/**
 * Notifications slice actions
 */
export const notificationsSlice = createSlice({
  name: 'notifications',
  initialState: initialNotificationsState,
  reducers: {
    /** Toggle notification center sidebar open/closed */
    toggleSidebar: (state) => {
      state.sidebarOpen = !state.sidebarOpen;
    },
    
    /** Explicitly set sidebar open state */
    setSidebarOpen: (state, action: PayloadAction<boolean>) => {
      state.sidebarOpen = action.payload;
    },
    
    /** Mark a notification as read */
    markAsRead: (state, action: PayloadAction<string>) => {
      const operationId = action.payload;
      if (!state.metadata[operationId]) {
        state.metadata[operationId] = {
          read: true,
          dismissed: false,
          lastInteractionTimestamp: Date.now(),
        };
      } else {
        state.metadata[operationId].read = true;
        state.metadata[operationId].lastInteractionTimestamp = Date.now();
      }
    },
    
    /** Dismiss a notification (hide from notification center) */
    dismissNotification: (state, action: PayloadAction<string>) => {
      const operationId = action.payload;
      if (!state.metadata[operationId]) {
        state.metadata[operationId] = {
          read: true,
          dismissed: true,
          lastInteractionTimestamp: Date.now(),
        };
      } else {
        state.metadata[operationId].dismissed = true;
        state.metadata[operationId].lastInteractionTimestamp = Date.now();
      }
    },
    
    /** Clear all completed/failed notifications (dismiss multiple) */
    clearAllCompleted: (state, action: PayloadAction<string[]>) => {
      const completedOperationIds = action.payload;
      completedOperationIds.forEach((id) => {
        if (!state.metadata[id]) {
          state.metadata[id] = {
            read: true,
            dismissed: true,
            lastInteractionTimestamp: Date.now(),
          };
        } else {
          state.metadata[id].dismissed = true;
          state.metadata[id].lastInteractionTimestamp = Date.now();
        }
      });
    },
    
    /** Perform 24-hour cleanup (remove old metadata) */
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
    
    /** Enable/disable polling (e.g., when browser tab inactive) */
    setPollingEnabled: (state, action: PayloadAction<boolean>) => {
      state.pollingEnabled = action.payload;
    },
  },
});
```

### Selectors

```typescript
/**
 * Selectors for notifications state
 */

/** Select whether sidebar is open */
export const selectSidebarOpen = (state: RootState): boolean => 
  state.notifications.sidebarOpen;

/** Select all operation metadata */
export const selectNotificationMetadata = (state: RootState): Record<string, NotificationMetadata> => 
  state.notifications.metadata;

/** Select metadata for specific operation */
export const selectOperationMetadata = (operationId: string) => (state: RootState): NotificationMetadata | undefined => 
  state.notifications.metadata[operationId];

/** Select whether polling is enabled */
export const selectPollingEnabled = (state: RootState): boolean => 
  state.notifications.pollingEnabled;

/**
 * Derived selector: Compute unread count for badge
 * 
 * Counts operations that are:
 * - NOT marked as read
 * - NOT dismissed
 * - Currently exist in RTK Query cache (from API)
 */
export const selectUnreadCount = createSelector(
  [
    selectNotificationMetadata,
    (state: RootState) => asyncOperationsApi.endpoints.getAsyncOperations.select()(state)?.data,
  ],
  (metadata, operations): number => {
    if (!operations) return 0;
    
    return operations.filter((op) => {
      const meta = metadata[op.id];
      return !meta?.read && !meta?.dismissed;
    }).length;
  }
);

/**
 * Derived selector: Get visible operations (not dismissed)
 * 
 * Filters out dismissed operations and sorts by recency
 */
export const selectVisibleOperations = createSelector(
  [
    selectNotificationMetadata,
    (state: RootState) => asyncOperationsApi.endpoints.getAsyncOperations.select()(state)?.data,
  ],
  (metadata, operations): AsyncOperation[] => {
    if (!operations) return [];
    
    return operations
      .filter((op) => !metadata[op.id]?.dismissed)
      .sort((a, b) => {
        // Sort by start time descending (most recent first)
        return new Date(b.startTimestamp).getTime() - new Date(a.startTimestamp).getTime();
      });
  }
);
```

## Relationships

```text
┌─────────────────────────────────────────────────────────────┐
│ Redux Store                                                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  notificationsSlice                                         │
│  ├── sidebarOpen: boolean                                   │
│  ├── metadata: Record<operationId, NotificationMetadata>   │
│  ├── lastCleanupTimestamp: number                           │
│  └── pollingEnabled: boolean                                │
│                                                             │
│  api (RTK Query)                                            │
│  └── endpoints                                              │
│       └── getAsyncOperations                                │
│            └── data: AsyncOperation[]  ← FROM SERVER       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                        │
                        │ Derived Data (Selectors)
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ Computed Views                                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  selectUnreadCount                                          │
│  ├── Input: metadata + operations                           │
│  └── Output: number (count for badge)                       │
│                                                             │
│  selectVisibleOperations                                    │
│  ├── Input: metadata + operations                           │
│  └── Output: AsyncOperation[] (filtered, sorted)           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow

### 1. Operation Initiation (Delete Button Click)

```text
User clicks delete
     │
     ▼
DeleteConfirmationModal
     │
     ▼
POST /api/world-entities/{id}/async-delete
     │
     ├─→ Frontend: Optimistically remove entity from hierarchy (immediate UI update)
     │   └─→ Update WorldSidebar Redux state to hide deleted entities
     │
     ▼
Server responds with { operationId: "..." }
     │
     ▼
Operation appears in next polling cycle (within 3 seconds)
```

**Note**: Frontend performs optimistic update by immediately removing the entity and its children from the displayed hierarchy while the backend processes the delete asynchronously. This provides immediate visual feedback without performance concerns since only loaded/expanded nodes in the UI are affected.

### 2. Polling Cycle (Every 3 Seconds)

```text
RTK Query polls GET /api/async-operations
     │
     ▼
Server returns AsyncOperation[]
     │
     ▼
RTK Query updates cache
     │
     ▼
Selectors recompute (unread count, visible operations)
     │
     ▼
UI components re-render (badge, notification list)
```

### 3. User Interaction (Mark as Read)

```text
User opens notification center
     │
     ▼
dispatch(markAsRead(operationId))
     │
     ▼
metadata[operationId].read = true
     │
     ▼
selectUnreadCount recomputes (badge count decreases)
     │
     ▼
Badge UI updates
```

## Validation Rules

### Client-Side Validation

1. **Operation ID**: Must be non-empty string (validated by TypeScript)
1. **Status**: Must be one of: 'pending' | 'in-progress' | 'completed' | 'failed'
1. **Progress Percentage**: Must be 0-100 when present
1. **Items Processed**: Must be ≤ Items Total
1. **Timestamps**: Must be valid ISO 8601 strings

### State Invariants

1. **Metadata keys match operation IDs**: All keys in `metadata` object should correspond to operation IDs from server
1. **Dismissed implies read**: If `dismissed === true`, then `read === true` (enforced in reducers)
1. **Cleanup runs periodically**: `performCleanup` action dispatched every hour; `lastCleanupTimestamp` updated
1. **Unread count accuracy**: `selectUnreadCount` always reflects current operations minus read/dismissed items

## Migration & Versioning

**Current Version**: 1.0 (initial implementation)

**Future Considerations**:

- If adding new operation types: Add to `AsyncOperationType` union (backward compatible)
- If adding new status states: Add to `AsyncOperationStatus` union (backward compatible)
- If changing metadata structure: Requires migration logic in reducer initialization (breaking change)

**Session Storage Only**: State does not persist across browser sessions, so no database migrations required. Schema changes only affect TypeScript types and Redux state shape.

## Summary

- **3 Core Types**: `AsyncOperation`, `NotificationMetadata`, Redux `NotificationsState`
- **1 Redux Slice**: `notificationsSlice` with 7 actions and 6 selectors
- **Session-Scoped**: All state in-memory; no backend persistence
- **Server-Driven**: `AsyncOperation` entities fetched from backend API via RTK Query
- **Client-Managed**: `NotificationMetadata` (read/dismissed) stored in Redux only
