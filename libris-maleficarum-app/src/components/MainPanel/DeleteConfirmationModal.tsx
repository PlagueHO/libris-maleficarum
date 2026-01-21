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
import { useDeleteWorldEntityMutation, useGetWorldEntityByIdQuery } from '@/services/worldEntityApi';
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
  const { deletingEntityId, selectedWorldId, showDeleteConfirmation } = useSelector(
    (state: RootState) => state.worldSidebar
  );

  const [deleteEntity, { isLoading }] = useDeleteWorldEntityMutation();

  // Fetch entity details to show name in confirmation
  const { data: entity } = useGetWorldEntityByIdQuery(
    { worldId: selectedWorldId!, entityId: deletingEntityId! },
    { skip: !selectedWorldId || !deletingEntityId }
  );

  const handleConfirmDelete = async () => {
    if (!selectedWorldId || !deletingEntityId) return;

    try {
      await deleteEntity({
        worldId: selectedWorldId,
        entityId: deletingEntityId,
      }).unwrap();

      dispatch(closeDeleteConfirmation());
    } catch (error) {
      console.error('Failed to delete entity:', error);
      // Error handling delegated to RTK Query error state
    }
  };

  const handleCancel = () => {
    dispatch(closeDeleteConfirmation());
  };

  return (
    <Dialog open={showDeleteConfirmation} onOpenChange={handleCancel}>
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

        <DialogFooter className="flex gap-3 justify-end pt-4 mt-4 border-t border-border sm:flex-row flex-col-reverse">
          <Button
            variant="outline"
            onClick={handleCancel}
            disabled={isLoading}
            aria-label="Cancel deletion"
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirmDelete}
            disabled={isLoading}
            aria-label={`Confirm delete "${entity?.name || 'entity'}"`}
          >
            {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isLoading ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
