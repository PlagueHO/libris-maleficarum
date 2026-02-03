# Feature Specification: Async Entity Operations with Notification Center

**Feature Branch**: `012-async-entity-operations`  
**Created**: February 3, 2026  
**Status**: Draft  
**Input**: User description: "Add support for asynchronous WorldEntity delete (with cascading) to the frontend React Application. This feature will need to include an async process notification button at the top right of the App window that will be able to use to track long running async processes like deletes (and also other processes in future). Therefore a mechanism/service in the front end for registering an async task for tracking will need to be implemented. This should be similar to how task and notifications work in the Azure Portal with a bell icon that can be clicked and all notifications and in process tasks are displayed in a sidebar. See image."

## Clarifications

### Session 2026-02-03

- Q: How should the frontend receive real-time status updates from the backend? → A: Periodic Polling (frontend polls backend API every 2-3 seconds for operation status updates). Implementation must use abstraction layer to allow future migration to Server-Sent Events, WebSocket, or SignalR without impacting consumer code.
- Q: Should completed notifications persist across browser sessions or only during the current session? → A: Session-only with 24-hour cap (keep notifications only in current browser session with automatic cleanup of completed operations older than 24 hours if browser remains open).
- Q: Should clicking outside the sidebar close it, or does it require explicit close action? → A: Click-outside closes (clicking anywhere outside sidebar or pressing ESC closes it, matching common modal patterns).
- Q: What format should progress indicators use? → A: Both percentage and count (show "X% complete • N/Total items processed" format, e.g., "45% complete • 120/267 entities deleted").
- Q: When a cascading delete encounters an error partway through, what should happen to already-deleted entities? → A: Partial commit with clear status (keep already-deleted entities deleted; show partial success with error details; retry continues from failure point).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Initiate Async Entity Delete (Priority: P1)

Users need to delete WorldEntity items (such as characters, locations, or entire world sections) without waiting for the operation to complete, especially when the delete involves cascading to child entities which may take several seconds or minutes.

**Why this priority**: This is the core functionality - without async delete capability, users experience UI blocking and poor responsiveness when deleting entities with many children. This is the minimum viable feature that delivers immediate value.

**Independent Test**: Can be fully tested by selecting any WorldEntity with children, clicking delete, and verifying the UI remains responsive while the delete processes in the background. Delivers value by immediately improving user experience during delete operations.

**Acceptance Scenarios**:

1. **Given** user has selected a WorldEntity with child entities, **When** user clicks the delete button and confirms, **Then** the delete operation initiates asynchronously and the UI remains responsive
1. **Given** user initiates an async delete, **When** the request is sent, **Then** user sees immediate confirmation that the operation has started
1. **Given** an async delete is in progress, **When** user navigates to different views, **Then** the delete continues processing without interruption
1. **Given** user initiates a delete of an entity with no children, **When** the operation completes quickly, **Then** user still receives consistent feedback through the notification system

---

### User Story 2 - Track Operation Progress via Notification Center (Priority: P2)

Users need visibility into async operations (starting with deletes) through a centralized notification center similar to Azure Portal, accessible via a bell icon in the top-right of the application.

**Why this priority**: Provides essential visibility and feedback for operations initiated in P1. Without this, users have no way to know if their delete succeeded or failed. This completes the minimum feedback loop required for async operations.

**Independent Test**: Can be tested by clicking the bell icon after initiating any async operation and verifying real-time status updates appear. Delivers value by providing transparency into background operations.

**Acceptance Scenarios**:

1. **Given** user has initiated one or more async operations, **When** user clicks the bell icon in top-right, **Then** a sidebar opens displaying all active and recent operations
1. **Given** an async delete is in progress, **When** user views the notification center, **Then** the operation shows current status (pending, in-progress, completed, failed) with progress indicator
1. **Given** no async operations have been initiated, **When** user clicks the bell icon, **Then** notification center shows empty state with helpful message
1. **Given** notification center is open, **When** an operation status changes, **Then** the display updates in real-time without requiring refresh
1. **Given** user has unread notifications, **When** viewing the app, **Then** the bell icon displays a badge count of unread items

---

### User Story 3 - View Operation History and Handle Errors (Priority: P3)

Users need to review completed operations, understand any failures, and take corrective action when operations fail (such as retrying or viewing detailed error information).

**Why this priority**: Enhances the P2 tracking by adding historical context and error recovery. While important for production use, the system can function without detailed history - users will still be notified of failures in real-time via P2.

**Independent Test**: Can be tested by completing multiple operations (successful and failed), then viewing notification center to verify all operations are listed with appropriate status and error details. Delivers value by enabling users to understand and recover from failures.

**Acceptance Scenarios**:

1. **Given** an async delete has completed successfully, **When** user views notification center, **Then** the operation shows completed status with summary (e.g., "Deleted World 'Fantasy Realm' and 47 child entities")
1. **Given** an async delete has failed, **When** user views notification center, **Then** the operation shows failed status with clear error message and suggested actions
1. **Given** user views a failed operation, **When** user clicks retry action, **Then** the operation re-initiates and updates status accordingly
1. **Given** multiple operations have completed, **When** user views notification center, **Then** operations are sorted by recency with most recent first
1. **Given** user has dismissed notifications, **When** user reopens notification center, **Then** dismissed items are hidden from main view but accessible via "View all" option

---

### User Story 4 - Handle Cascading Entity Deletes (Priority: P4)

When users delete a parent WorldEntity, the system must delete all descendant entities in the hierarchy while providing clear feedback about the scope of the operation.

**Why this priority**: This is specific to the delete operation's complexity rather than the async notification system itself. While critical for data integrity, it's an enhancement to the basic delete in P1. The notification system (P1-P3) works equally well whether deletes are simple or cascading.

**Independent Test**: Can be tested by deleting a top-level entity (e.g., Continent) and verifying all children (Countries, Regions, Cities, Characters) are deleted correctly with appropriate progress updates. Delivers value by ensuring data consistency and preventing orphaned entities.

**Acceptance Scenarios**:

1. **Given** user initiates delete of parent entity with descendants, **When** confirming delete, **Then** user sees warning showing total count of entities to be deleted (e.g., "This will delete 1 continent, 3 countries, 12 regions, and 89 related entities")
1. **Given** cascading delete is in progress, **When** user views operation status, **Then** progress shows "X% complete • N/Total entities deleted" format (e.g., "45% complete • 120/267 entities deleted")
1. **Given** cascading delete encounters an error on a child entity, **When** the error occurs, **Then** the operation shows partial success status with count of successfully deleted entities and specific error details (e.g., "45 of 100 entities deleted before error: [error message]")
1. **Given** cascading delete completes successfully, **When** user views the entity hierarchy, **Then** all parent and child entities are removed from the UI

---

### Edge Cases

- What happens when user closes the browser tab while an async delete is in progress? (Operation continues on server; notification state lost and must be retrieved from backend API when user returns in new session)
- How does system handle concurrent deletes of parent and child entities? (Server-side validation prevents conflicts; user receives clear error if conflict detected)
- What happens when network connectivity is lost during an async operation? (Notification shows connection lost; polling resumes when connectivity restored)
- How does system handle very long-running operations (e.g., deleting a world with 10,000+ entities)? (Progress updates continue; operation state persists in backend; frontend can retrieve current state on page refresh or new session)
- What happens when user tries to edit an entity that's queued for deletion? (UI prevents edits; shows warning that entity is being deleted)
- How are notifications managed when user has dozens of completed operations? (Session-only storage with automatic cleanup of completed operations older than 24 hours if browser remains open; users can manually clear all or individually; new session starts fresh)
- What happens to already-deleted entities if a cascading delete fails partway through? (Already-deleted entities remain deleted; operation shows partial success with count; retry continues from failure point without reprocessing successfully deleted entities)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST initiate WorldEntity delete operations asynchronously without blocking the UI
- **FR-002**: System MUST provide a notification center accessible via bell icon in top-right corner of the application window
- **FR-003**: System MUST display notification center in a sidebar panel that overlays the main content
- **FR-004**: System MUST track async operation status with states: pending, in-progress, completed, failed
- **FR-005**: System MUST update operation status in real-time as progress occurs using periodic polling (every 2-3 seconds) with abstraction layer that allows future migration to push-based mechanisms (SSE, WebSocket, SignalR) without impacting consumer code
- **FR-006**: System MUST persist notification state during user session only (in-memory, not across browser sessions) and maintain state across page navigation within app
- **FR-007**: System MUST display unread notification count as badge on bell icon
- **FR-008**: System MUST provide operation history showing all operations from current session with automatic cleanup of completed operations older than 24 hours
- **FR-009**: System MUST show estimated progress for long-running operations in format "X% complete • N/Total items processed" (e.g., "45% complete • 120/267 entities deleted")
- **FR-010**: System MUST handle cascading deletes by removing all descendant WorldEntity items when parent is deleted with partial commit semantics (already-deleted entities remain deleted if error occurs)
- **FR-011**: System MUST provide confirmation dialog before delete showing scope of cascading operation
- **FR-012**: System MUST display clear error messages when async operations fail, including suggested corrective actions and partial completion status (count of successfully deleted entities before failure)
- **FR-013**: System MUST allow users to dismiss individual notifications or clear all completed operations
- **FR-014**: System MUST provide retry capability for failed operations directly from notification center with retry continuing from failure point (not reprocessing already-deleted entities)
- **FR-015**: System MUST prevent user actions on entities that are queued for deletion or being deleted
- **FR-016**: System MUST support extensible async operation types beyond delete (foundation for future operations)
- **FR-017**: System MUST maintain notification visibility when users navigate between different views in the application
- **FR-018**: System MUST provide notification sorting with most recent operations first
- **FR-019**: Notification center MUST be accessible via keyboard (focus management, ESC to close) and MUST close when user clicks outside the sidebar panel or presses ESC key
- **FR-020**: Notification center MUST announce status updates to screen readers for accessibility

### Key Entities *(include if feature involves data)*

- **AsyncOperation**: Represents a background operation being tracked; attributes include unique ID, operation type (DELETE, etc.), target entity reference, status (pending/in-progress/completed/failed), progress percentage, start timestamp, completion timestamp, error details (if failed)
- **Notification**: User-facing message derived from AsyncOperation; attributes include unique ID, message text, severity (info/success/warning/error), timestamp, read status, dismissed status, associated operation ID, actionable options (retry, view details, dismiss)
- **OperationResult**: Outcome of completed operation; attributes include success status, affected entity count, error message(s), detailed logs, retry count

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can initiate delete operations and continue working immediately without UI blocking or wait times
- **SC-002**: Users receive real-time operation status updates within 2 seconds of status changes on the server
- **SC-003**: Users can view and understand the status of all async operations (current and recent) in a single notification center view
- **SC-004**: 100% of async delete operations provide clear feedback on completion or failure
- **SC-005**: Users can complete a full workflow of initiating delete, checking status, and viewing results within 30 seconds of operation completion
- **SC-006**: Cascading deletes correctly remove all descendant entities with zero orphaned records remaining
- **SC-007**: Users can identify unread notifications at a glance via bell icon badge count
- **SC-008**: Notification center meets WCAG 2.2 Level AA accessibility standards (keyboard navigation, screen reader support)
- **SC-009**: Failed operations provide actionable error messages with retry success rate above 80% (when retry is applicable)
- **SC-010**: System handles at least 10 concurrent async operations per user without performance degradation
