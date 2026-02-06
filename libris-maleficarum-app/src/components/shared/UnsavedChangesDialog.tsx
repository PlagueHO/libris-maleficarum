/**
 * UnsavedChangesDialog Component
 *
 * Confirmation dialog shown when user attempts to navigate away or trigger another
 * action while the current form has unsaved changes.
 *
 * Provides three options:
 * - Save: Save changes and proceed with navigation
 * - Don't Save: Discard changes and proceed with navigation
 * - Cancel: Abort navigation, stay in current form
 *
 * @module components/shared/UnsavedChangesDialog
 * @see specs/008-edit-world-entity/contracts/UnsavedChangesDialog.contract.ts
 */

import { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Loader2 } from 'lucide-react';

export interface UnsavedChangesDialogProps {
  /** Whether the dialog is open */
  open: boolean;

  /**
   * Callback when "Save" button is clicked
   * Should return a Promise that resolves when save completes
   * or rejects if save fails
   */
  onSave: () => Promise<void>;

  /**
   * Callback when "Don't Save" button is clicked
   * Discards changes and proceeds with pending navigation
   */
  onDiscard: () => void;

  /**
   * Callback when "Cancel" button is clicked or dialog is dismissed (Escape key)
   * Aborts navigation, keeps form open with unsaved changes
   */
  onCancel: () => void;

  /** Loading state during async save operation (optional) */
  isSaving?: boolean;
}

/**
 * UnsavedChangesDialog component
 *
 * @param props - Component props
 * @returns Dialog UI for unsaved changes confirmation
 */
export function UnsavedChangesDialog({
  open,
  onSave,
  onDiscard,
  onCancel,
  isSaving = false,
}: UnsavedChangesDialogProps) {
  const [isInternalSaving, setIsInternalSaving] = useState(false);
  const effectiveSaving = isSaving || isInternalSaving;

  const handleSave = async () => {
    setIsInternalSaving(true);
    try {
      await onSave();
    } catch {
      // Error is handled by parent component (toast notification)
      // Dialog stays open for retry
    } finally {
      setIsInternalSaving(false);
    }
  };

  const handleOpenChange = (isOpen: boolean) => {
    // Only allow closing via Cancel button or Escape key
    if (!isOpen && !effectiveSaving) {
      onCancel();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        className="max-w-md"
        aria-labelledby="unsaved-changes-title"
        aria-describedby="unsaved-changes-description"
      >
        <DialogHeader>
          <DialogTitle id="unsaved-changes-title">
            Unsealed Changes
          </DialogTitle>
          <DialogDescription id="unsaved-changes-description">
            Your inscription is not yet sealed. Would you like to preserve your work before continuing?
          </DialogDescription>
        </DialogHeader>

        <DialogFooter className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={effectiveSaving}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={onDiscard}
            disabled={effectiveSaving}
          >
            Don't Save
          </Button>
          <Button
            type="button"
            variant="default"
            onClick={handleSave}
            disabled={effectiveSaving}
            autoFocus
          >
            {effectiveSaving ? (
              <>
                <Loader2 className="animate-spin" aria-hidden="true" />
                Saving...
              </>
            ) : (
              'Save'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
