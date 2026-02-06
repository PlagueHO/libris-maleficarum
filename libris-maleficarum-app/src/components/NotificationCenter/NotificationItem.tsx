/**
 * NotificationItem Component
 * 
 * Individual notification item showing async operation status, progress, and actions.
 * Displays in the NotificationCenter list.
 * 
 * Features:
 * - Entity name and operation type
 * - Status icon (pending, in-progress, completed, failed)
 * - Progress bar for in-progress operations
 * - Unread indicator (blue left border)
 * - Click to mark as read
 * - Dismiss button
 * - Keyboard accessible
 * 
 * @module NotificationCenter/NotificationItem
 */

import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Clock, Loader2, CheckCircle, XCircle, X, RotateCw, StopCircle } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '@/store/store';
import { markAsRead, dismissNotification, selectOperationMetadata } from '@/store/notificationsSlice';
import { useRetryDeleteOperationMutation, useCancelDeleteOperationMutation } from '@/services/asyncOperationsApi';
import { getOperationStatusMessage } from '@/lib/asyncOperationHelpers';
import type { DeleteOperationDto } from '@/services/types/asyncOperations';
import { cn } from '@/lib/utils';
import { useWorldOptional } from '@/contexts';

export interface NotificationItemProps {
  /**
   * Async operation to display
   */
  operation: DeleteOperationDto;
}

/**
 * NotificationItem component
 * 
 * @example
 * ```tsx
 * <NotificationItem operation={operation} />
 * ```
 */
export function NotificationItem({ operation }: NotificationItemProps) {
  const worldContext = useWorldOptional();
  // Use context worldId when available, fall back to the operation's own worldId
  const worldId = worldContext?.worldId || operation.worldId;
  const dispatch = useAppDispatch();
  const metadata = useAppSelector(selectOperationMetadata(operation.id));
  const [retryOperation, { isLoading: isRetrying }] = useRetryDeleteOperationMutation();
  const [cancelOperation, { isLoading: isCancelling }] = useCancelDeleteOperationMutation();
  
  const isUnread = !metadata?.isRead;
  
  // Status icon mapping
  const statusIcon = {
    pending: <Clock className="h-4 w-4 text-yellow-500" aria-hidden="true" />,
    in_progress: <Loader2 className="h-4 w-4 animate-spin text-blue-500" aria-hidden="true" />,
    completed: <CheckCircle className="h-4 w-4 text-green-600" aria-hidden="true" />,
    failed: <XCircle className="h-4 w-4 text-red-600" aria-hidden="true" />,
    partial: <XCircle className="h-4 w-4 text-orange-600" aria-hidden="true" />,
  }[operation.status];
  
  const handleClick = () => {
    if (isUnread) {
      dispatch(markAsRead(operation.id));
    }
  };
  
  const handleDismiss = (e: React.MouseEvent) => {
    e.stopPropagation();
    dispatch(dismissNotification(operation.id));
  };
  
  const handleRetry = async (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await retryOperation({ worldId, operationId: operation.id }).unwrap();
      // Operation status will update via polling
    } catch (error) {
      console.error('Retry failed:', error);
    }
  };
  
  const handleCancel = async (e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await cancelOperation({ worldId, operationId: operation.id }).unwrap();
      // Operation status will update via polling
    } catch (error) {
      console.error('Cancel failed:', error);
    }
  };
  
  // Show retry button only for failed or partial operations
  const showRetryButton = operation.status === 'failed' || operation.status === 'partial';
  // Show cancel button only for in-progress operations
  const showCancelButton = operation.status === 'in_progress';
  
  return (
    <Card
      className={cn(
        'p-3 transition-colors relative',
        isUnread && 'border-l-4 border-l-blue-500'
      )}
      data-testid="notification-item"
    >
      {/* Main content - clickable area for marking as read */}
      <div
        className="cursor-pointer hover:bg-accent/50 rounded -m-3 p-3 pr-10 pb-2"
        onClick={handleClick}
        aria-label={`Mark ${operation.rootEntityName} notification as read`}
      >
        <div className="flex items-start gap-3">
          {/* Status Icon */}
          <div className="mt-0.5">{statusIcon}</div>
          
          {/* Content */}
          <div className="flex-1 min-w-0">
            {/* Entity Name */}
            <p className="font-medium truncate">{operation.rootEntityName}</p>
            
            {/* Status Message */}
            <p className="text-sm text-muted-foreground">
              {getOperationStatusMessage(operation)}
            </p>
            
            {/* Deleted Count (for in-progress or completed operations) */}
            {operation.deletedCount > 0 && (
              <p className="text-xs text-muted-foreground mt-1">
                Banished: {operation.deletedCount} entries
              </p>
            )}
          </div>
        </div>
      </div>
      
      {/* Action Buttons - outside clickable area to avoid nested interactives */}
      {(showRetryButton || showCancelButton) && (
        <div className="mt-2 flex gap-2 ml-7">
          {/* Retry Button (failed/partial operations only) */}
          {showRetryButton && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleRetry}
              disabled={isRetrying}
              className="text-xs"
              aria-label="Retry operation"
            >
              <RotateCw className={cn("h-3 w-3 mr-1", isRetrying && "animate-spin")} aria-hidden="true" />
              {isRetrying ? 'Retrying...' : 'Retry'}
            </Button>
          )}
          
          {/* Cancel Button (in-progress operations only) */}
          {showCancelButton && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleCancel}
              disabled={isCancelling}
              className="text-xs"
              aria-label="Cancel operation"
            >
              <StopCircle className="h-3 w-3 mr-1" aria-hidden="true" />
              {isCancelling ? 'Cancelling...' : 'Cancel'}
            </Button>
          )}
        </div>
      )}
      
      {/* Dismiss Button - positioned absolutely */}
      <Button
        variant="ghost"
        size="icon"
        className="h-6 w-6 shrink-0 absolute top-3 right-3"
        onClick={handleDismiss}
        aria-label="Dismiss notification"
        data-testid="dismiss-button"
      >
        <X className="h-4 w-4" aria-hidden="true" />
      </Button>
    </Card>
  );
}
