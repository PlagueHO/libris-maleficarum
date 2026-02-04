/**
 * Type definitions for delete operations
 *
 * These types align with the backend DeleteOperationResponse DTO.
 * All types use backend naming conventions (camelCase for JSON properties).
 *
 * @module asyncOperations
 */

/**
 * Delete operation status values from backend
 * Maps to DeleteOperationStatus enum in backend (converted to snake_case via ToApiString())
 */
export type DeleteOperationStatus =
  | 'pending'
  | 'in_progress'
  | 'completed'
  | 'partial'
  | 'failed';

/**
 * Delete operation DTO from backend API
 * Maps to: DeleteOperationResponse.cs
 *
 * @see {@link https://github.com/PlagueHO/libris-maleficarum/blob/main/libris-maleficarum-service/src/Api/Models/Responses/DeleteOperationResponse.cs}
 */
export interface DeleteOperationDto {
  /**
   * Unique operation identifier (server-generated GUID)
   */
  id: string;

  /**
   * World identifier this operation belongs to
   */
  worldId: string;

  /**
   * Root entity ID being deleted
   */
  rootEntityId: string;

  /**
   * Name of the root entity (for display purposes)
   */
  rootEntityName: string;

  /**
   * Current status of the operation
   */
  status: DeleteOperationStatus;

  /**
   * Total number of entities to delete (including root and all descendants)
   */
  totalEntities: number;

  /**
   * Number of entities successfully deleted so far
   */
  deletedCount: number;

  /**
   * Number of entities that failed to delete
   */
  failedCount: number;

  /**
   * List of entity IDs that failed to delete (for retry/debugging)
   */
  failedEntityIds: string[] | null;

  /**
   * Error details if the operation failed
   */
  errorDetails: string | null;

  /**
   * Whether cascade delete is enabled for this operation
   */
  cascade: boolean;

  /**
   * User ID who initiated this operation
   */
  createdBy: string;

  /**
   * ISO 8601 timestamp when operation was created
   */
  createdAt: string;

  /**
   * ISO 8601 timestamp when operation started processing
   */
  startedAt: string | null;

  /**
   * ISO 8601 timestamp when operation completed (success or failure)
   */
  completedAt: string | null;
}

/**
 * Client-side metadata for UI state (not from backend)
 * Tracks user interactions with notifications
 */
export interface DeleteOperationMetadata {
  /**
   * Operation ID (matches DeleteOperationDto.id)
   */
  operationId: string;

  /**
   * Whether user has viewed/read this notification
   */
  isRead: boolean;

  /**
   * Whether user has dismissed this notification from view
   */
  isDismissed: boolean;

  /**
   * Unix timestamp (ms) when user last interacted with notification
   */
  lastInteractionTimestamp: number;
}

/**
 * Combined state for Redux store
 */
export interface DeleteOperationsState {
  /**
   * Notification center sidebar open state
   */
  sidebarOpen: boolean;

  /**
   * Client-side metadata for each operation notification
   * Key: Operation ID
   */
  metadata: Record<string, DeleteOperationMetadata>;

  /**
   * Unix timestamp (ms) of last cleanup run
   */
  lastCleanupTimestamp: number;

  /**
   * Polling control flag
   */
  pollingEnabled: boolean;
}
