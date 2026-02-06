/**
 * DeleteConfirmationModal Component
 *
 * Modal dialog for confirming entity deletion.
 * Only used for delete operations - other operations use main panel forms.
 *
 * Features:
 * - Shows entity name in confirmation message
 * - Offers Cancel and Delete options
 * - Handles deletion via API mutation
 * - Shows loading state during deletion
 *
 * @module components/MainPanel/DeleteConfirmationModal
 */

import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '@/store/store';
import { closeDeleteConfirmation } from '@/store/worldSidebarSlice';
import { useInitiateEntityDeleteMutation } from '@/services/asyncOperationsApi';
import { useOptimisticDelete } from '@/components/WorldSidebar/OptimisticDeleteContext';
import { logger } from '@/lib/logger';
import { toast } from 'sonner';
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { AlertTriangle, Loader2 } from 'lucide-react';

/**
 * Determines if an RTK Query error should trigger an optimistic update rollback.
 * 
 * Rollback cases:
 * - Client errors (4xx): Request was rejected
 * - FETCH_ERROR: Network failure (no response from server)
 * - TIMEOUT_ERROR: Request timed out before completion
 * - PARSING_ERROR: Response received but couldn't be parsed
 * 
 * No rollback cases:
 * - Server errors (5xx): Operation may have been queued despite error
 * - Unknown errors: Conservative approach - assume operation may have succeeded
 */
function shouldRollbackDelete(error: unknown): boolean {
  // Type guard for FetchBaseQueryError
  const fetchError = error as FetchBaseQueryError | undefined;
  const status = fetchError?.status;
  
  if (typeof status === 'number') {
    // HTTP status code - rollback only for client errors (4xx)
    // Server errors (5xx) may indicate the operation was queued
    return status >= 400 && status < 500;
  }
  
  if (typeof status === 'string') {
    // RTK Query string status types that indicate operation failure
    return status === 'FETCH_ERROR' || status === 'TIMEOUT_ERROR' || status === 'PARSING_ERROR';
  }
  
  // Unknown error type - don't rollback (conservative approach)
  return false;
}

/**
 * Generates appropriate error message based on error type.
 */
function getErrorMessage(error: unknown, entityName: string | undefined): { title: string; description: string } {
  const isKnownFailure = shouldRollbackDelete(error);
  
  if (isKnownFailure) {
    return {
      title: 'The deletion has failed',
      description: `"${entityName || 'Entry'}" resists erasure. Please check the entry and attempt the rite again.`,
    };
  }
  
  return {
    title: 'The deletion outcome is uncertain',
    description: `"${entityName || 'Entry'}" may already be processing. Please check the notification center or refresh the sidebar to verify.`,
  };
}

export function DeleteConfirmationModal() {
  const dispatch = useDispatch();
  const { deletingEntityId, deletingEntityName, showDeleteConfirmation, selectedWorldId } = useSelector(
    (state: RootState) => state.worldSidebar
  );
  
  const { onOptimisticDelete, onRollbackDelete } = useOptimisticDelete();

  const [initiateAsyncDelete, { isLoading: isInitiatingAsync }] = useInitiateEntityDeleteMutation();

  const handleConfirmDelete = async () => {

    if (!selectedWorldId || !deletingEntityId) {
      logger.error('UI', 'Delete aborted - missing required data', {
        selectedWorldId,
        deletingEntityId,
      });
      return;
    }

    logger.userAction('Confirm delete entity', {
      entityId: deletingEntityId,
      entityName: deletingEntityName,
    });

    try {
      // 1. Optimistic UI update (remove from sidebar immediately)
      onOptimisticDelete(deletingEntityId);
      
      // 2. Initiate async delete on backend
      await initiateAsyncDelete({ 
        worldId: selectedWorldId, 
        entityId: deletingEntityId,
        cascade: true
      }).unwrap();

      // 3. Close dialog immediately - operation continues in background
      dispatch(closeDeleteConfirmation());
      
      // Note: Notification will be registered automatically by RTK Query cache update
    } catch (error) {
      logger.error('API', 'Failed to initiate async delete', {
        entityId: deletingEntityId,
        error,
      });
      
      // Rollback optimistic update if the error indicates the operation wasn't accepted
      if (shouldRollbackDelete(error)) {
        onRollbackDelete(deletingEntityId);
      }
      
      // Close dialog to allow user to retry or verify state
      dispatch(closeDeleteConfirmation());
      
      // Show appropriate error message based on error type
      const { title, description } = getErrorMessage(error, deletingEntityName ?? undefined);
      toast.error(title, { description });
    }
  };
  
  const isDeleting = isInitiatingAsync;

  const handleCancel = () => {
    dispatch(closeDeleteConfirmation());
  };

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      handleCancel();
    }
  };

  return (
    <Dialog open={showDeleteConfirmation} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-xl font-semibold mb-1">
            <AlertTriangle className="h-5 w-5 text-destructive" aria-hidden="true" />
            Delete Entry
          </DialogTitle>
          <DialogDescription className="text-[0.95rem] text-muted-foreground leading-normal">
            Are you certain you wish to delete{' '}
            <strong>"{deletingEntityName || 'this entry'}"</strong> from the grimoire?
          </DialogDescription>
        </DialogHeader>

        <div 
          className="bg-destructive/10 border border-destructive/30 rounded-md px-4 py-3 my-4"
          role="alert" 
          aria-live="polite"
        >
          <p className="m-0 text-sm text-foreground">
            This rite cannot be undone. The entry and all its descendants will be permanently erased from the tome.
          </p>
        </div>

        <DialogFooter className="gap-2">
          <Button
            variant="outline"
            onClick={handleCancel}
            disabled={isDeleting}
            aria-label="Cancel deletion"
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirmDelete}
            disabled={isDeleting}
            aria-label={`Confirm delete "${deletingEntityName || 'entry'}"`}
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
