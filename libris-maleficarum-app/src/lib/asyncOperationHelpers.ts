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
      return 'Awaiting the rite';

    case 'in_progress':
      if (operation.totalEntities > 0) {
        const percent = Math.round((operation.deletedCount / operation.totalEntities) * 100);
        return `${percent}% complete • ${operation.deletedCount}/${operation.totalEntities} entries banished`;
      }
      return 'The incantation is underway...';

    case 'completed':
      return `Rite completed • ${operation.deletedCount} ${
        operation.deletedCount === 1 ? 'entry' : 'entries'
      } banished from the tome`;

    case 'partial':
      return `Rite partially completed • ${operation.deletedCount} banished, ${operation.failedCount} resisted`;

    case 'failed':
      return operation.errorDetails || 'The rite has failed';

    default:
      return 'Status unknown — the tome is silent';
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
    return `${diffSeconds} seconds ago`;
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
