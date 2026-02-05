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
      logger.error('API', 'Failed to initiate async delete', {
        entityId: deletingEntityId,
        error,
      });
      
      // Rollback optimistic update on error (restore entity to UI)
      onRollbackDelete(deletingEntityId);
      
      // Close dialog to allow user to retry or cancel
      dispatch(closeDeleteConfirmation());
      
      // Show error toast so the user knows the delete failed
      toast.error('Failed to delete entity', {
        description: `"${entity?.name || 'Entity'}" could not be deleted. Please try again.`,
      });
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
          <DialogTitle className="text-xl font-semibold mb-1">Delete Entity</DialogTitle>
          <DialogDescription className="text-[0.95rem] text-muted-foreground leading-normal">
            Are you sure you want to delete{' '}
            <strong>"{entity?.name || 'this entity'}"</strong>?
          </DialogDescription>
        </DialogHeader>

        <div 
          className="bg-destructive/10 border border-destructive/30 rounded-md px-4 py-3 my-4"
          role="alert" 
          aria-live="polite"
        >
          <p className="m-0 text-sm text-foreground">
            This action cannot be undone. The entity and all its child entities will be permanently deleted.
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
            aria-label={`Confirm delete "${entity?.name || 'entity'}"`}
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
