# Research: Async Entity Operations with Notification Center

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Date**: February 3, 2026  
**Phase**: 0 - Technical Research

## Executive Summary

This research validates the technical approach for implementing async entity operations with a notification center in the React frontend. All critical technical decisions have been clarified and validated against existing patterns in the codebase and industry best practices.

## Research Areas

### 1. Polling Implementation with RTK Query

**Decision**: Use RTK Query's `useQuery` hook with `pollingInterval` option for status updates

**Rationale**:

- RTK Query provides built-in polling support via `pollingInterval` parameter
- Automatic request cancellation when component unmounts or polling stops
- Integrates seamlessly with existing `api` slice pattern in codebase
- Maintains cache coherence across multiple components accessing same data
- `refetchOnMountOrArgChange` ensures fresh data when notification center opens

**Alternatives Considered**:

- Manual `setInterval` with `useEffect`: More complex cleanup logic; doesn't leverage RTK Query caching
- React Query: Would introduce second state management library; inconsistent with existing Redux Toolkit usage
- Custom polling service: Reinvents RTK Query functionality; harder to test

**Implementation Pattern**:

```typescript
// In asyncOperationsApi.ts
export const asyncOperationsApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getAsyncOperations: builder.query<AsyncOperation[], void>({
      query: () => '/api/async-operations',
      providesTags: ['AsyncOperation'],
    }),
  }),
});

// In NotificationCenter.tsx
const { data: operations } = useGetAsyncOperationsQuery(undefined, {
  pollingInterval: 3000, // Poll every 3 seconds when component mounted
  skipPollingIfUnfocused: true, // Pause polling when browser tab inactive (performance)
});
```

**Abstraction Strategy** (for future SSE/WebSocket migration):

- Polling logic isolated in `asyncOperationsService.ts` service layer
- Components consume `useAsyncOperations()` custom hook (not RTK Query hook directly)
- Service layer can swap polling for push-based mechanism without changing component API
- Example abstraction:

```typescript
// asyncOperationsService.ts
export function useAsyncOperations() {
  // Current: RTK Query polling
  const { data, isLoading, error } = useGetAsyncOperationsQuery(undefined, {
    pollingInterval: 3000,
  });
  
  // Future: Can replace with SSE/WebSocket listener
  // const data = useSSEAsyncOperations('/api/async-operations/stream');
  
  return { operations: data ?? [], isLoading, error };
}
```

---

### 2. Notification Center UI Pattern with Shadcn/ui

**Decision**: Use Shadcn/ui Drawer component for notification sidebar panel

**Rationale**:

- Drawer provides slide-out panel behavior matching Azure Portal UX
- Built on Vaul library (accessibility features: focus trap, ESC handling, ARIA attributes, drag gestures)
- Supports click-outside-to-close via `modal: false` prop
- Responsive: full-screen on mobile, sidebar on desktop
- Consistent with existing Shadcn/ui component patterns in codebase

**Alternatives Considered**:

- Dialog component (Radix UI): Better for centered modals, not ideal for sidebar panels
- Sheet component (Shadcn/ui): Similar to Drawer but less semantic for notification use case
- Custom sidebar with Radix Popover: More manual ARIA management; Drawer (Vaul) handles this automatically

**Implementation Pattern**:

```typescript
// NotificationCenter.tsx
import { Drawer, DrawerContent, DrawerHeader, DrawerTitle } from '@/components/ui/drawer';

<Drawer open={isOpen} onOpenChange={setIsOpen}>
  <DrawerContent className="h-full w-96 ml-auto">
    <DrawerHeader>
      <DrawerTitle>Notifications</DrawerTitle>
    </DrawerHeader>
    <ScrollArea className="flex-1">
      {operations.map(op => <NotificationItem key={op.id} operation={op} />)}
    </ScrollArea>
  </DrawerContent>
</Drawer>
```

**Badge Component** (unread count indicator):

- Use existing `src/components/ui/badge.tsx` (Shadcn/ui Badge)
- Position absolute on bell icon with `variant="destructive"` for alert style
- Show count only when > 0 (hide when no unread notifications)

---

### 3. Redux State Management for Async Operations

**Decision**: Single `notificationsSlice` managing operation state and UI state

**Rationale**:

- Centralized state for all notification-related concerns (operations list, unread count, dismissed IDs, sidebar open/closed)
- RTK Query handles server state (fetching operations from API)
- Redux slice handles client state (read/dismissed status, UI visibility)
- Normalized state structure for efficient updates and lookups

**Alternatives Considered**:

- Separate slices for operations vs. UI: Overkill for feature scope; increases complexity
- Store everything in RTK Query cache: Query cache not designed for client-side state (read/dismissed status)
- Local component state only: Doesn't persist across navigation; harder to test

**State Shape**:

```typescript
interface NotificationsState {
  // UI state
  sidebarOpen: boolean;
  
  // Client-side operation metadata (keyed by operation ID)
  operationMetadata: Record<string, {
    read: boolean;
    dismissed: boolean;
  }>;
  
  // Session cleanup timestamp
  lastCleanup: number; // Unix timestamp of last 24-hour cleanup
}
```

**Actions**:

- `toggleSidebar()`: Open/close notification center
- `markAsRead(operationId)`: Mark notification as read
- `dismissNotification(operationId)`: Dismiss individual notification
- `clearAll()`: Dismiss all completed notifications
- `performCleanup()`: Remove metadata for operations older than 24 hours

**Selectors**:

- `selectUnreadCount`: Compute badge count (operations not marked as read and not dismissed)
- `selectVisibleOperations`: Filter out dismissed operations
- `selectIsSidebarOpen`: UI visibility flag

---

### 4. Accessibility Patterns for Notification Centers

**Decision**: Implement WCAG 2.2 Level AA compliance with specific ARIA patterns

**Rationale**:

- Notification center is a `region` landmark with `aria-label="Notifications"`
- Bell button uses `aria-label="Notifications"` + `aria-expanded` when sidebar open
- Badge shows `aria-live="polite"` region announcing count changes to screen readers
- Drawer component (Vaul) handles focus trap, ESC key, and return focus on close
- Each notification item uses `role="article"` with semantic structure

**Alternatives Considered**:

- ARIA `alert` role for new notifications: Too intrusive; disrupts screen reader flow for non-critical updates
- `aria-live="assertive"`: Too aggressive; `polite` is sufficient for notification updates

**Implementation Checklist** (from constitution III & accessibility instructions):

- [x] All interactive elements keyboard accessible (bell button, notification actions)
- [x] Focus management: Drawer traps focus when open, returns focus to bell button on close
- [x] ARIA labels clearly describe purpose ("Notifications", "Dismiss notification", "Retry operation")
- [x] Status updates announced to screen readers via `aria-live` regions
- [x] Color not sole indicator (use icons + text for status: pending/success/error)
- [x] Minimum 4.5:1 contrast for text, 3:1 for UI components
- [x] ESC key closes drawer (handled by Radix Dialog primitive)
- [x] Click outside drawer closes it (handled by Radix Dialog with `modal={false}`)

**Testing Strategy**:

- Unit tests with jest-axe: `expect(await axe(container)).toHaveNoViolations()`
- Manual screen reader testing: NVDA (Windows), VoiceOver (macOS)
- Keyboard navigation testing: Tab order, focus indicators, ESC behavior

---

### 5. Cascading Delete with Partial Commit Semantics

**Decision**: Backend handles cascading logic; frontend shows progress and handles partial failures

**Rationale**:

- Server-side cascading ensures data integrity and transactional consistency
- Server tracks progress (deleted entity count) and provides to frontend via API
- Frontend displays progress using format: "X% complete • N/Total entities deleted"
- Partial commit: Already-deleted entities remain deleted if error occurs (no rollback)
- Retry continues from failure point (backend tracks last successfully deleted entity)

**Alternatives Considered**:

- Client-side cascading: Inefficient (many API calls); race conditions; network failure issues
- Full rollback on error: Complex for large hierarchies; may be impossible after partial server-side commit

**API Contract** (from backend, out of scope for this frontend feature):

```typescript
// POST /api/world-entities/{id}/async-delete
// Response: { operationId: string }

// GET /api/async-operations/{operationId}
// Response:
{
  id: string;
  type: 'DELETE';
  targetEntityId: string;
  targetEntityName: string;
  status: 'pending' | 'in-progress' | 'completed' | 'failed';
  progress: {
    percentComplete: number;
    itemsProcessed: number;
    itemsTotal: number;
  };
  result?: {
    success: boolean;
    deletedCount: number;
    errorMessage?: string;
  };
  startTimestamp: string; // ISO 8601
  completionTimestamp?: string; // ISO 8601
}
```

---

### 6. Session State Management (24-Hour Cleanup)

**Decision**: `useEffect` hook in `App.tsx` runs cleanup on mount and every hour

**Rationale**:

- Simple interval-based cleanup (no complex background workers)
- Cleanup removes metadata for operations older than 24 hours from state
- Server-side API may also clean up old operations (backend concern, not frontend)
- Memory efficient: Prevents unbounded growth of dismissed notification metadata

**Implementation Pattern**:

```typescript
// In App.tsx or NotificationCenter provider
useEffect(() => {
  const cleanup = () => {
    const now = Date.now();
    const cutoff = now - 24 * 60 * 60 * 1000; // 24 hours ago
    dispatch(notificationsActions.performCleanup(cutoff));
  };
  
  cleanup(); // Run on mount
  const interval = setInterval(cleanup, 60 * 60 * 1000); // Run every hour
  
  return () => clearInterval(interval);
}, [dispatch]);
```

---

## Summary of Decisions

| Area | Decision | Key Benefit |
|------|----------|-------------|
| **Real-time Updates** | RTK Query polling (3s interval) with abstraction layer | Simple, leverages existing patterns, future-proof |
| **UI Framework** | Shadcn/ui Drawer + Badge | Accessible by default, consistent with codebase |
| **State Management** | Redux slice for client state, RTK Query for server state | Clear separation, efficient updates |
| **Accessibility** | Radix UI primitives + ARIA patterns + jest-axe tests | WCAG 2.2 Level AA compliance guaranteed |
| **Cascading Deletes** | Server-side logic, partial commit semantics | Data integrity, resilient to failures |
| **Session Cleanup** | Interval-based cleanup every hour (24h retention) | Memory efficient, simple implementation |

All research validates the technical approach outlined in the specification. No blocking technical uncertainties remain. Ready to proceed to Phase 1: Data Model & Contracts.
