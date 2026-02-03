# Developer Quickstart: Async Entity Operations with Notification Center

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Data Model**: [data-model.md](./data-model.md) | **API Contract**: [contracts/async-operations-api.yaml](./contracts/async-operations-api.yaml)  
**Date**: February 3, 2026  
**Phase**: 1 - Implementation Guide

## Overview

This quickstart guide helps developers implement the async entity operations feature with notification center in the React frontend. Follow the order below for test-driven development (TDD) per constitution principle III.

## Development Workflow

**Total Effort**: ~3-4 days (1 developer)

```text
Day 1: Redux State + Service Layer (P1 foundation)
Day 2: Notification Center UI Components (P2 visibility)
Day 3: Integration + Delete Flow (P1+P2 integration)
Day 4: Error Handling + Polish (P3 error recovery, P4 cascading)
```

## Prerequisites

- [ ] Familiarize with existing codebase:
  - [ ] Read `libris-maleficarum-app/src/store/store.ts` (Redux setup)
  - [ ] Read `libris-maleficarum-app/src/services/api.ts` (RTK Query patterns)
  - [ ] Review `libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx` (toolbar structure)
  - [ ] Review shadcn/ui Drawer and Badge docs: <https://ui.shadcn.com/docs/components/drawer>

- [ ] Confirm backend API endpoints are available (or use MSW mocks for development):
  - [ ] `POST /api/world-entities/{id}/async-delete`
  - [ ] `GET /api/async-operations`
  - [ ] `GET /api/async-operations/{operationId}`
  - [ ] `POST /api/async-operations/{operationId}/retry`

## Step-by-Step Implementation

### Phase 1: Redux State Layer (Day 1 - Morning)

#### 1.1 Create Redux Slice - Test First! ✅

**File**: `libris-maleficarum-app/src/store/notificationsSlice.test.ts`

```typescript
import { describe, it, expect } from 'vitest';
import notificationsReducer, { 
  toggleSidebar, 
  markAsRead, 
  dismissNotification,
  clearAllCompleted,
  performCleanup,
} from './notificationsSlice';
import type { NotificationsState } from './notificationsSlice';

describe('notificationsSlice', () => {
  const initialState: NotificationsState = {
    sidebarOpen: false,
    metadata: {},
    lastCleanupTimestamp: Date.now(),
    pollingEnabled: true,
  };

  it('should toggle sidebar state', () => {
    const state = notificationsReducer(initialState, toggleSidebar());
    expect(state.sidebarOpen).toBe(true);
    
    const state2 = notificationsReducer(state, toggleSidebar());
    expect(state2.sidebarOpen).toBe(false);
  });

  it('should mark notification as read', () => {
    const operationId = 'op-123';
    const state = notificationsReducer(initialState, markAsRead(operationId));
    
    expect(state.metadata[operationId]).toBeDefined();
    expect(state.metadata[operationId].read).toBe(true);
    expect(state.metadata[operationId].dismissed).toBe(false);
  });

  it('should dismiss notification', () => {
    const operationId = 'op-123';
    const state = notificationsReducer(initialState, dismissNotification(operationId));
    
    expect(state.metadata[operationId].dismissed).toBe(true);
    expect(state.metadata[operationId].read).toBe(true); // Dismissed implies read
  });

  it('should clear all completed notifications', () => {
    const state = notificationsReducer(initialState, clearAllCompleted(['op-1', 'op-2']));
    
    expect(state.metadata['op-1'].dismissed).toBe(true);
    expect(state.metadata['op-2'].dismissed).toBe(true);
  });

  it('should perform cleanup of old metadata', () => {
    const oneHourAgo = Date.now() - (60 * 60 * 1000);
    const twentyFiveHoursAgo = Date.now() - (25 * 60 * 60 * 1000);
    
    const stateWithOldMeta: NotificationsState = {
      ...initialState,
      metadata: {
        'op-recent': { read: true, dismissed: false, lastInteractionTimestamp: oneHourAgo },
        'op-old': { read: true, dismissed: true, lastInteractionTimestamp: twentyFiveHoursAgo },
      },
    };
    
    const cutoff = Date.now() - (24 * 60 * 60 * 1000); // 24 hours ago
    const state = notificationsReducer(stateWithOldMeta, performCleanup(cutoff));
    
    expect(state.metadata['op-recent']).toBeDefined(); // Recent kept
    expect(state.metadata['op-old']).toBeUndefined(); // Old removed
  });
});
```

**Run test**: `pnpm test src/store/notificationsSlice.test.ts`  
**Expected**: All tests fail (red) - slice doesn't exist yet

#### 1.2 Implement Redux Slice

**File**: `libris-maleficarum-app/src/store/notificationsSlice.ts`

```typescript
import { createSlice, createSelector } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { RootState } from './store';
import { asyncOperationsApi } from '@/services/asyncOperationsApi';

export interface NotificationMetadata {
  read: boolean;
  dismissed: boolean;
  lastInteractionTimestamp: number;
}

export interface NotificationsState {
  sidebarOpen: boolean;
  metadata: Record<string, NotificationMetadata>;
  lastCleanupTimestamp: number;
  pollingEnabled: boolean;
}

const initialState: NotificationsState = {
  sidebarOpen: false,
  metadata: {},
  lastCleanupTimestamp: Date.now(),
  pollingEnabled: true,
};

const notificationsSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    toggleSidebar: (state) => {
      state.sidebarOpen = !state.sidebarOpen;
    },
    setSidebarOpen: (state, action: PayloadAction<boolean>) => {
      state.sidebarOpen = action.payload;
    },
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
        state.metadata[operationId].read = true; // Dismissed implies read
        state.metadata[operationId].lastInteractionTimestamp = Date.now();
      }
    },
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
          state.metadata[id].read = true;
          state.metadata[id].lastInteractionTimestamp = Date.now();
        }
      });
    },
    performCleanup: (state, action: PayloadAction<number>) => {
      const cutoffTimestamp = action.payload;
      const now = Date.now();
      
      Object.keys(state.metadata).forEach((operationId) => {
        const meta = state.metadata[operationId];
        if (meta.lastInteractionTimestamp < cutoffTimestamp) {
          delete state.metadata[operationId];
        }
      });
      
      state.lastCleanupTimestamp = now;
    },
    setPollingEnabled: (state, action: PayloadAction<boolean>) => {
      state.pollingEnabled = action.payload;
    },
  },
});

export const {
  toggleSidebar,
  setSidebarOpen,
  markAsRead,
  dismissNotification,
  clearAllCompleted,
  performCleanup,
  setPollingEnabled,
} = notificationsSlice.actions;

// Selectors
export const selectSidebarOpen = (state: RootState) => state.notifications.sidebarOpen;
export const selectNotificationMetadata = (state: RootState) => state.notifications.metadata;
export const selectPollingEnabled = (state: RootState) => state.notifications.pollingEnabled;

// Derived selectors
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

export const selectVisibleOperations = createSelector(
  [
    selectNotificationMetadata,
    (state: RootState) => asyncOperationsApi.endpoints.getAsyncOperations.select()(state)?.data,
  ],
  (metadata, operations) => {
    if (!operations) return [];
    
    return operations
      .filter((op) => !metadata[op.id]?.dismissed)
      .sort((a, b) => new Date(b.startTimestamp).getTime() - new Date(a.startTimestamp).getTime());
  }
);

export default notificationsSlice.reducer;
```

**Run test**: `pnpm test src/store/notificationsSlice.test.ts`  
**Expected**: All tests pass (green)

#### 1.3 Register Slice in Store

**File**: `libris-maleficarum-app/src/store/store.ts`

```typescript
// Add import
import notificationsReducer from './notificationsSlice';

// Add to reducer object
export const store = configureStore({
  reducer: {
    sidePanel: sidePanelSlice.reducer,
    worldSidebar: worldSidebarReducer,
    notifications: notificationsReducer, // <- ADD THIS
    [api.reducerPath]: api.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});
```

### Phase 2: API Service Layer (Day 1 - Afternoon)

#### 2.1 Create RTK Query Endpoints - Test First! ✅

**File**: `libris-maleficarum-app/src/services/asyncOperationsApi.test.ts`

```typescript
import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { store } from '@/store/store';
import { asyncOperationsApi } from './asyncOperationsApi';

const server = setupServer(
  http.get('/api/async-operations', () => {
    return HttpResponse.json({
      operations: [
        {
          id: 'op-1',
          type: 'DELETE',
          targetEntityId: 'entity-1',
          targetEntityName: 'Test World',
          targetEntityType: 'World',
          status: 'in-progress',
          progress: { percentComplete: 45, itemsProcessed: 120, itemsTotal: 267 },
          result: null,
          startTimestamp: '2026-02-03T08:00:00Z',
          completionTimestamp: null,
        },
      ],
      totalCount: 1,
    });
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('asyncOperationsApi', () => {
  it('should fetch async operations', async () => {
    const result = await store.dispatch(
      asyncOperationsApi.endpoints.getAsyncOperations.initiate()
    );
    
    expect(result.data?.operations).toHaveLength(1);
    expect(result.data?.operations[0].id).toBe('op-1');
  });
});
```

**Run test**: `pnpm test src/services/asyncOperationsApi.test.ts`  
**Expected**: Test fails (red) - API file doesn't exist

#### 2.2 Implement RTK Query API

**File**: `libris-maleficarum-app/src/services/asyncOperationsApi.ts`

```typescript
import { api } from './api';
import type {
  AsyncOperation,
  AsyncOperationStatus,
  AsyncOperationType,
} from './types/asyncOperations';

export const asyncOperationsApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getAsyncOperations: builder.query<
      { operations: AsyncOperation[]; totalCount: number },
      { status?: AsyncOperationStatus; type?: AsyncOperationType; limit?: number } | void
    >({
      query: (params) => ({
        url: '/async-operations',
        method: 'GET',
        params,
      }),
      providesTags: ['AsyncOperation'],
    }),
    
    getAsyncOperation: builder.query<AsyncOperation, string>({
      query: (operationId) => `/async-operations/${operationId}`,
      providesTags: (result, error, operationId) => [
        { type: 'AsyncOperation', id: operationId },
      ],
    }),
    
    retryAsyncOperation: builder.mutation<AsyncOperation, string>({
      query: (operationId) => ({
        url: `/async-operations/${operationId}/retry`,
        method: 'POST',
      }),
      invalidatesTags: (result, error, operationId) => [
        { type: 'AsyncOperation', id: operationId },
        'AsyncOperation',
      ],
    }),
    
    initiateAsyncDelete: builder.mutation<
      { operationId: string; targetEntityId: string; targetEntityName: string; estimatedCount: number },
      string
    >({
      query: (entityId) => ({
        url: `/world-entities/${entityId}/async-delete`,
        method: 'POST',
      }),
      invalidatesTags: ['AsyncOperation', 'WorldEntity'],
    }),
  }),
});

export const {
  useGetAsyncOperationsQuery,
  useGetAsyncOperationQuery,
  useRetryAsyncOperationMutation,
  useInitiateAsyncDeleteMutation,
} = asyncOperationsApi;
```

**File**: `libris-maleficarum-app/src/services/types/asyncOperations.ts`

```typescript
// Copy type definitions from data-model.md
export type AsyncOperationStatus = 'pending' | 'in-progress' | 'completed' | 'failed' | 'cancelled';
export type AsyncOperationType = 'DELETE' | 'CREATE' | 'UPDATE' | 'IMPORT' | 'EXPORT';

export interface AsyncOperationProgress {
  percentComplete: number;
  itemsProcessed: number;
  itemsTotal: number;
  currentItem?: string;
}

export interface AsyncOperationResult {
  success: boolean;
  affectedCount: number;
  errorMessage?: string;
  errorDetails?: {
    code: string;
    failedAtItem?: string;
    stackTrace?: string;
  };
  retryCount: number;
}

export interface AsyncOperation {
  id: string;
  type: AsyncOperationType;
  targetEntityId: string;
  targetEntityName: string;
  targetEntityType: string;
  status: AsyncOperationStatus;
  progress: AsyncOperationProgress | null;
  result: AsyncOperationResult | null;
  startTimestamp: string;
  completionTimestamp: string | null;
}
```

**Run test**: `pnpm test src/services/asyncOperationsApi.test.ts`  
**Expected**: Tests pass (green)

#### 2.3 Update RTK Query Tag Types

**File**: `libris-maleficarum-app/src/services/api.ts`

```typescript
export const api = createApi({
  reducerPath: 'api',
  baseQuery: axiosBaseQuery(),
  tagTypes: [
    'World',
    'WorldEntity',
    'Character',
    'Location',
    'Organization',
    'AsyncOperation', // <- ADD THIS
  ],
  // ...rest
});
```

### Phase 3: UI Components (Days 2-3)

See `plan.md` Project Structure section for complete component list. Follow TDD:

1. Write component test with jest-axe accessibility checks
1. Run test (red)
1. Implement component
1. Run test (green)
1. Refactor

**Key Components Priority**:

1. `NotificationBell.tsx` (P1 - trigger for sidebar)
1. `NotificationCenter.tsx` (P2 - sidebar panel)
1. `NotificationItem.tsx` (P2 - individual notification)
1. Integration with `DeleteConfirmationModal.tsx` (P1 - async delete initiation)

### Phase 4: Integration & Testing (Day 4)

#### 4.1 Integration Tests

**File**: `libris-maleficarum-app/src/__tests__/integration/asyncDeleteWorkflow.test.tsx`

Test P1-P4 user stories with MSW mocking full workflow.

#### 4.2 Accessibility Testing

All component tests MUST include:

```typescript
import { axe, toHaveNoViolations } from 'jest-axe';
expect.extend(toHaveNoViolations);

it('should have no accessibility violations', async () => {
  const { container } = render(<NotificationCenter />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

## Testing Checklist

- [ ] Unit tests for Redux slice (all actions)
- [ ] Unit tests for selectors (unread count, visible operations)
- [ ] API tests with MSW mocking
- [ ] Component tests with React Testing Library
- [ ] Accessibility tests with jest-axe (WCAG 2.2 Level AA)
- [ ] Integration tests for P1-P4 user stories
- [ ] Visual regression tests (Playwright)

## Running the Feature

```bash
# Terminal 1: Start frontend dev server
cd libris-maleficarum-app
pnpm dev

# Terminal 2: Run tests in watch mode (TDD)
pnpm test --watch

# Terminal 3: Start backend (when available) or use MSW mocks
cd ../libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

Access app at: <https://127.0.0.1:4000>

## Common Issues & Solutions

**Issue**: Polling never stops  
**Solution**: Ensure `skipPollingIfUnfocused: true` in `useGetAsyncOperationsQuery`

**Issue**: Badge count incorrect  
**Solution**: Check `selectUnreadCount` selector logic; ensure dismissed operations filtered

**Issue**: Accessibility violations  
**Solution**: Run `pnpm test:ui` and review jest-axe output; ensure all ARIA labels present

## Next Steps

After implementation complete:

1. Run `/speckit.tasks` to generate task breakdown
1. Create PR with branch `012-async-entity-operations`
1. Ensure all tests pass in CI
1. Request review with accessibility checklist

## References

- [Spec](./spec.md) - Feature requirements
- [Data Model](./data-model.md) - TypeScript types
- [API Contract](./contracts/async-operations-api.yaml) - OpenAPI spec
- [Research](./research.md) - Technical decisions
- [RTK Query Polling](https://redux-toolkit.js.org/rtk-query/usage/polling)
- [Shadcn/ui Drawer](https://ui.shadcn.com/docs/components/drawer)
- [WCAG 2.2](https://www.w3.org/TR/WCAG22/)
