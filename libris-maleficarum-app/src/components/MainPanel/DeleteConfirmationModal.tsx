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
import { useGetWorldEntityByIdQuery } from '@/services/worldEntityApi';
import { useInitiateEntityDeleteMutation } from '@/services/asyncOperationsApi';
import { useOptimisticDelete } from '@/components/WorldSidebar/OptimisticDeleteContext';
import { useWorldOptional } from '@/contexts';
import { logger } from '@/lib/logger';
import { toast } from 'sonner';
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

export function DeleteConfirmationModal() {
  const dispatch = useDispatch();
  const worldContext = useWorldOptional();
  const { deletingEntityId, showDeleteConfirmation } = useSelector(
    (state: RootState) => state.worldSidebar
  );
  
  const { onOptimisticDelete, onRollbackDelete } = useOptimisticDelete();

  const [initiateAsyncDelete, { isLoading: isInitiatingAsync }] = useInitiateEntityDeleteMutation();

  // Fetch entity details to show name in confirmation
  const { data: entity } = useGetWorldEntityByIdQuery(
    { 
      worldId: worldContext?.worldId || '', 
      entityId: deletingEntityId || '' 
    },
    { skip: !worldContext?.worldId || !deletingEntityId }
  );

  const handleConfirmDelete = async () => {

    if (!worldContext?.worldId || !deletingEntityId) {
      logger.error('UI', 'Delete aborted - missing required data', {
        hasWorldContext: !!worldContext,
        worldId: worldContext?.worldId,
        deletingEntityId,
      });
      return;
    }

    logger.userAction('Confirm delete entity', {
      entityId: deletingEntityId,
      entityName: entity?.name,
    });

    try {
      // 1. Optimistic UI update (remove from sidebar immediately)
      onOptimisticDelete(deletingEntityId);
      
      // 2. Initiate async delete on backend
      await initiateAsyncDelete({ 
        worldId: worldContext.worldId, 
        entityId: deletingEntityId,
        cascade: true
      }).unwrap();

      // 3. Close dialog immediately - operation continues in background
      dispatch(closeDeleteConfirmation());
      
      // Note: Notification will be registered automatically by RTK Query cache update
    } catch (error) {
      // Check error type to determine appropriate rollback and messaging strategy
      // RTK Query unwrap() can throw FetchBaseQueryError or SerializedError
      const errorObject = (typeof error === 'object' && error !== null) ? (error as { status?: unknown }) : undefined;
      const rawStatus = errorObject?.status;
      const numericStatus = typeof rawStatus === 'number' ? rawStatus : undefined;
      const isClientError = typeof numericStatus === 'number' && numericStatus >= 400 && numericStatus < 500;
      const isNetworkError = rawStatus === 'FETCH_ERROR';
      
      logger.error('API', 'Failed to initiate async delete', {
        entityId: deletingEntityId,
        status: rawStatus,
        error,
      });
      
      // For client errors (4xx) or network errors, we know the delete wasn't accepted
      // Roll back the optimistic update so the entity remains visible
      // For server errors (5xx), the operation may have been queued - keep optimistic update
      if (isClientError || isNetworkError) {
        onRollbackDelete(deletingEntityId);
      }
      
      // Close dialog to allow user to retry or verify state
      dispatch(closeDeleteConfirmation());
      
      // Show appropriate error message based on error type
      if (isClientError || isNetworkError) {
        toast.error('The banishment has failed', {
          description: `"${entity?.name || 'Entry'}" resists erasure. Please check the entry and attempt the rite again.`,
        });
      } else {
        toast.error('The banishment outcome is uncertain', {
          description: `"${entity?.name || 'Entry'}" may already be processing. Please check the notification center or refresh the sidebar to verify.`,
        });
      }
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
          <div className="flex mb-2">
            <AlertTriangle className="h-5 w-5 text-destructive" aria-hidden="true" />
          </div>
          <DialogTitle className="text-xl font-semibold mb-1">Banish This Entry</DialogTitle>
          <DialogDescription className="text-[0.95rem] text-muted-foreground leading-normal">
            Are you certain you wish to banish{' '}
            <strong>"{entity?.name || 'this entry'}"</strong> from the grimoire?
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
            aria-label={`Confirm banish "${entity?.name || 'entry'}"`}
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
