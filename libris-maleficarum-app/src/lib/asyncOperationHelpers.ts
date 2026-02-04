/**
 * Delete Operation Helper Utilities
 *
 * Utility functions for formatting delete operation data for display in the
 * notification center UI. Includes status messages, progress formatting,
 * time formatting, and icon helpers.
 *
 * @module asyncOperationHelpers
 */

import type {
  DeleteOperationDto,
  DeleteOperationStatus,
} from '@/services/types/asyncOperations';

/**
 * Get user-friendly status message for a delete operation
 *
 * Returns a concise, human-readable message describing the current
 * state of the operation.
 *
 * @example
 * getOperationStatusMessage(pendingOp) // "Queued for processing"
 * getOperationStatusMessage(inProgressOp) // "45% complete • 120/267 entities"
 * getOperationStatusMessage(completedOp) // "Completed successfully"
 */
export function getOperationStatusMessage(operation: DeleteOperationDto): string {
  switch (operation.status) {
    case 'pending':
      return 'Queued for processing';

    case 'in_progress':
      if (operation.totalEntities > 0) {
        const percent = Math.round((operation.deletedCount / operation.totalEntities) * 100);
        return `${percent}% complete • ${operation.deletedCount}/${operation.totalEntities} entities`;
      }
      return 'Processing...';

    case 'completed':
      return `Completed successfully • ${operation.deletedCount} ${
        operation.deletedCount === 1 ? 'entity' : 'entities'
      } deleted`;

    case 'partial':
      return `Partially completed • ${operation.deletedCount} deleted, ${operation.failedCount} failed`;

    case 'failed':
      return operation.errorDetails || 'Operation failed';

    default:
      return 'Unknown status';
  }
}

/**
 * Get operation type display name (always "Delete" for delete operations)
 */
export function getOperationTypeLabel(): string {
  return 'Delete';
}

/**
 * Get operation action description
 *
 * Returns a verb phrase describing what the operation is doing.
 *
 * @example
 * getOperationActionLabel('pending') // "Deleting"
 * getOperationActionLabel('completed') // "Deleted"
 */
export function getOperationActionLabel(status: DeleteOperationStatus): string {
  const isPastTense =
    status === 'completed' || status === 'failed' || status === 'partial';

  return isPastTense ? 'Deleted' : 'Deleting';
}

/**
 * Get icon name for operation (always trash icon for delete)
 */
export function getOperationIcon(): string {
  return 'trash';
}

/**
 * Get icon name for status
 *
 * Returns a semantic icon identifier for the operation status.
 *
 * @example
 * getStatusIcon('pending') // "clock"
 * getStatusIcon('in_progress') // "spinner"
 * getStatusIcon('completed') // "check-circle"
 * getStatusIcon('failed') // "x-circle"
 */
export function getStatusIcon(status: DeleteOperationStatus): string {
  switch (status) {
    case 'pending':
      return 'clock';
    case 'in_progress':
      return 'spinner';
    case 'completed':
      return 'check-circle';
    case 'partial':
      return 'alert-circle';
    case 'failed':
      return 'x-circle';
    default:
      return 'help-circle';
  }
}

/**
 * Get Tailwind CSS color class for status
 *
 * Returns a status-appropriate color class for styling.
 *
 * @example
 * getStatusColorClass('pending') // "text-yellow-600"
 * getStatusColorClass('completed') // "text-green-600"
 * getStatusColorClass('failed') // "text-red-600"
 */
export function getStatusColorClass(status: DeleteOperationStatus): string {
  switch (status) {
    case 'pending':
      return 'text-yellow-600';
    case 'in_progress':
      return 'text-blue-600';
    case 'completed':
      return 'text-green-600';
    case 'partial':
      return 'text-orange-600';
    case 'failed':
      return 'text-red-600';
    default:
      return 'text-gray-600';
  }
}

/**
 * Format elapsed time since operation started
 *
 * Returns a human-readable relative time string.
 *
 * @example
 * formatElapsedTime(operation) // "2 minutes ago"
 * formatElapsedTime(operation) // "just now"
 */
export function formatElapsedTime(operation: DeleteOperationDto): string {
  const timestamp = operation.startedAt || operation.createdAt;
  const startDate = new Date(timestamp);
  const now = new Date();
  const diffMs = now.getTime() - startDate.getTime();
  const diffSeconds = Math.floor(diffMs / 1000);
  const diffMinutes = Math.floor(diffSeconds / 60);
  const diffHours = Math.floor(diffMinutes / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffSeconds < 10) {
    return 'just now';
  } else if (diffSeconds < 60) {
    return `${diffSeconds} ${diffSeconds === 1 ? 'second' : 'seconds'} ago`;
  } else if (diffMinutes < 60) {
    return `${diffMinutes} ${diffMinutes === 1 ? 'minute' : 'minutes'} ago`;
  } else if (diffHours < 24) {
    return `${diffHours} ${diffHours === 1 ? 'hour' : 'hours'} ago`;
  } else {
    return `${diffDays} ${diffDays === 1 ? 'day' : 'days'} ago`;
  }
}

/**
 * Format ISO 8601 timestamp to local time string
 *
 * @example
 * formatTimestamp("2026-02-04T10:30:00Z") // "2/4/2026, 10:30:00 AM"
 */
export function formatTimestamp(timestamp: string | null): string {
  if (!timestamp) return 'N/A';

  const date = new Date(timestamp);
  return date.toLocaleString();
}

/**
 * Format duration between two timestamps
 *
 * @example
 * formatDuration(startedAt, completedAt) // "2m 34s"
 */
export function formatDuration(start: string | null, end: string | null): string {
  if (!start || !end) return 'N/A';

  const startDate = new Date(start);
  const endDate = new Date(end);
  const diffMs = endDate.getTime() - startDate.getTime();
  const diffSeconds = Math.floor(diffMs / 1000);
  const minutes = Math.floor(diffSeconds / 60);
  const seconds = diffSeconds % 60;

  if (minutes > 0) {
    return `${minutes}m ${seconds}s`;
  }
  return `${seconds}s`;
}

/**
 * Check if operation is in a terminal state (completed, failed, partial)
 *
 * Terminal operations will not receive further updates from the backend.
 */
export function isOperationTerminal(operation: DeleteOperationDto): boolean {
  return (
    operation.status === 'completed' ||
    operation.status === 'failed' ||
    operation.status === 'partial'
  );
}

/**
 * Check if operation is currently active (pending or in progress)
 *
 * Active operations should be polled for status updates.
 */
export function isOperationActive(operation: DeleteOperationDto): boolean {
  return operation.status === 'pending' || operation.status === 'in_progress';
}

/**
 * Check if operation can be retried
 *
 * Only failed or partial operations can be retried.
 */
export function canRetryOperation(operation: DeleteOperationDto): boolean {
  return operation.status === 'failed' || operation.status === 'partial';
}

/**
 * Get progress percentage for a delete operation
 *
 * @param operation - The delete operation
 * @returns Progress percentage (0-100)
 */
export function getProgressPercentage(operation: DeleteOperationDto): number {
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
    case 'CREATE':
      return isPastTense ? 'Created' : 'Creating';
    case 'UPDATE':
      return isPastTense ? 'Updated' : 'Updating';
    case 'IMPORT':
      return isPastTense ? 'Imported' : 'Importing';
    case 'EXPORT':
      return isPastTense ? 'Exported' : 'Exporting';
    default:
      return isPastTense ? 'Processed' : 'Processing';
  }
}

/**
 * Get icon name for operation type
 *
 * Returns a Lucide icon name appropriate for the operation type.
 * Used for rendering icons in the notification center.
 *
 * @example
 * getOperationIcon('DELETE') // "trash-2"
 * getOperationIcon('CREATE') // "plus-circle"
 */
export function getOperationIcon(type: AsyncOperationType): string {
  switch (type) {
    case 'DELETE':
      return 'trash-2';
    case 'CREATE':
      return 'plus-circle';
    case 'UPDATE':
      return 'edit';
    case 'IMPORT':
      return 'download';
    case 'EXPORT':
      return 'upload';
    default:
      return 'activity';
  }
}

/**
 * Get icon name for operation status
 *
 * Returns a Lucide icon name appropriate for the operation status.
 * Used for rendering status indicators in the notification center.
 *
 * @example
 * getStatusIcon('in-progress') // "loader-2" (with spin animation)
 * getStatusIcon('completed') // "check-circle"
 * getStatusIcon('failed') // "x-circle"
 */
export function getStatusIcon(status: AsyncOperationStatus): string {
  switch (status) {
    case 'pending':
      return 'clock';
    case 'in-progress':
      return 'loader-2'; // Spinner icon (apply spin animation in component)
    case 'completed':
      return 'check-circle';
    case 'failed':
      return 'x-circle';
    case 'cancelled':
      return 'ban';
    default:
      return 'help-circle';
  }
}

/**
 * Get CSS color class for operation status
 *
 * Returns TailwindCSS color class for status indicators.
 *
 * @example
 * getStatusColorClass('completed') // "text-green-500"
 * getStatusColorClass('failed') // "text-red-500"
 */
export function getStatusColorClass(status: AsyncOperationStatus): string {
  switch (status) {
    case 'pending':
      return 'text-yellow-500';
    case 'in-progress':
      return 'text-blue-500';
    case 'completed':
      return 'text-green-500';
    case 'failed':
      return 'text-red-500';
    case 'cancelled':
      return 'text-gray-500';
    default:
      return 'text-gray-400';
  }
}

/**
 * Format elapsed time since operation started
 *
 * Returns a human-readable relative time string.
 *
 * @example
 * formatElapsedTime(startTimestamp) // "2 minutes ago"
 * formatElapsedTime(startTimestamp) // "just now"
 */
export function formatElapsedTime(startTimestamp: string): string {
  const start = new Date(startTimestamp).getTime();
  const now = Date.now();
  const elapsedMs = now - start;
  const elapsedSeconds = Math.floor(elapsedMs / 1000);

  if (elapsedSeconds < 10) {
    return 'just now';
  }

  if (elapsedSeconds < 60) {
    return `${elapsedSeconds} seconds ago`;
  }

  const elapsedMinutes = Math.floor(elapsedSeconds / 60);
  if (elapsedMinutes < 60) {
    return `${elapsedMinutes} ${elapsedMinutes === 1 ? 'minute' : 'minutes'} ago`;
  }

  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) {
    return `${elapsedHours} ${elapsedHours === 1 ? 'hour' : 'hours'} ago`;
  }

  const elapsedDays = Math.floor(elapsedHours / 24);
  return `${elapsedDays} ${elapsedDays === 1 ? 'day' : 'days'} ago`;
}

/**
 * Format operation duration
 *
 * Returns a human-readable duration string for completed/failed operations.
 *
 * @example
 * formatOperationDuration(startTime, endTime) // "2m 30s"
 * formatOperationDuration(startTime, endTime) // "45s"
 */
export function formatOperationDuration(
  startTimestamp: string,
  completionTimestamp: string | null
): string {
  if (!completionTimestamp) {
    return 'In progress';
  }

  const start = new Date(startTimestamp).getTime();
  const end = new Date(completionTimestamp).getTime();
  const durationMs = end - start;
  const durationSeconds = Math.floor(durationMs / 1000);

  if (durationSeconds < 60) {
    return `${durationSeconds}s`;
  }

  const minutes = Math.floor(durationSeconds / 60);
  const seconds = durationSeconds % 60;

  if (seconds === 0) {
    return `${minutes}m`;
  }

  return `${minutes}m ${seconds}s`;
}

/**
 * Check if operation is in terminal state
 *
 * Returns true if operation has reached a final state (completed/failed/cancelled).
 *
 * @example
 * isOperationTerminal(completedOp) // true
 * isOperationTerminal(inProgressOp) // false
 */
export function isOperationTerminal(operation: AsyncOperation): boolean {
  return (
    operation.status === 'completed' ||
    operation.status === 'failed' ||
    operation.status === 'cancelled'
  );
}

/**
 * Check if operation is in active state
 *
 * Returns true if operation is pending or in-progress.
 *
 * @example
 * isOperationActive(inProgressOp) // true
 * isOperationActive(completedOp) // false
 */
export function isOperationActive(operation: AsyncOperation): boolean {
  return operation.status === 'pending' || operation.status === 'in-progress';
}

/**
 * Check if operation can be retried
 *
 * Returns true if operation is in failed state and can be retried.
 *
 * @example
 * canRetryOperation(failedOp) // true
 * canRetryOperation(completedOp) // false
 */
export function canRetryOperation(operation: AsyncOperation): boolean {
  return operation.status === 'failed';
}

/**
 * Check if operation can be cancelled
 *
 * Returns true if operation is pending or in-progress.
 *
 * @example
 * canCancelOperation(inProgressOp) // true
 * canCancelOperation(completedOp) // false
 */
export function canCancelOperation(operation: AsyncOperation): boolean {
  return operation.status === 'pending' || operation.status === 'in-progress';
}

/**
 * Get progress percentage as integer
 *
 * Returns rounded progress percentage, or 0 if no progress data.
 *
 * @example
 * getProgressPercentage(operation) // 45
 */
export function getProgressPercentage(operation: AsyncOperation): number {
  return operation.progress ? Math.round(operation.progress.percentComplete) : 0;
}

/**
 * Get estimated time remaining
 *
 * Returns a rough estimate of time remaining based on current progress rate.
 * Returns null if not enough data to estimate.
 *
 * @example
 * getEstimatedTimeRemaining(operation) // "~2 minutes remaining"
 * getEstimatedTimeRemaining(operation) // null (not enough data)
 */
export function getEstimatedTimeRemaining(operation: AsyncOperation): string | null {
  if (!operation.progress || operation.status !== 'in-progress') {
    return null;
  }

  const { percentComplete } = operation.progress;

  if (percentComplete <= 0) {
    return null;
  }

  // Calculate elapsed time
  const start = new Date(operation.startTimestamp).getTime();
  const now = Date.now();
  const elapsedMs = now - start;

  // Estimate total time based on current progress
  const estimatedTotalMs = (elapsedMs / percentComplete) * 100;
  const remainingMs = estimatedTotalMs - elapsedMs;

  const remainingSeconds = Math.floor(remainingMs / 1000);

  if (remainingSeconds < 60) {
    return `~${remainingSeconds} seconds remaining`;
  }

  const remainingMinutes = Math.floor(remainingSeconds / 60);
  return `~${remainingMinutes} ${remainingMinutes === 1 ? 'minute' : 'minutes'} remaining`;
}
