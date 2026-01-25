/**
 * UnsavedChangesDialog Component Contract
 *
 * Confirmation dialog shown when user attempts to navigate away or trigger another
 * action while the current form has unsaved changes.
 *
 * Provides three options:
 * - Yes (Save): Save changes and proceed with navigation
 * - No (Don't Save): Discard changes and proceed with navigation
 * - Cancel: Abort navigation, stay in current form
 *
 * @module components/MainPanel/UnsavedChangesDialog
 */

/**
 * UnsavedChangesDialog component properties
 */
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
 * Dialog structure (using Shadcn/ui Dialog)
 */
export interface UnsavedChangesDialogStructure {
  /** Root component */
  component: 'Dialog' /* from @/components/ui/dialog */;

  /** Controlled open state */
  open: boolean;

  /** Close handler */
  onOpenChange: '(isOpen: boolean) => !isOpen && onCancel()';

  /** Content structure */
  content: {
    title: 'Unsaved Changes';
    description: 'You have unsaved changes. Would you like to save them before continuing?';

    /** Footer with action buttons */
    footer: {
      /** Button order: Cancel, Don't Save, Save (primary) */
      buttons: [
        {
          label: 'Cancel';
          variant: 'outline';
          onClick: '() => onCancel()';
          disabled: 'isSaving';
        },
        {
          label: "Don't Save";
          variant: 'secondary';
          onClick: '() => onDiscard()';
          disabled: 'isSaving';
        },
        {
          label: 'Save';
          variant: 'default' /* Primary button */;
          onClick: '() => onSave()';
          disabled: 'isSaving';
          loadingIndicator: 'Loader2 component when isSaving=true';
        }
      ];
    };
  };
}

/**
 * Dialog behavior and user interactions
 */
export interface UnsavedChangesDialogBehavior {
  /** Opening conditions */
  trigger: {
    condition: 'hasUnsavedChanges === true && user initiates navigation';
    examples: [
      'User clicks edit icon for different entity',
      'User clicks close/back button',
      'User selects different entity in hierarchy'
    ];
  };

  /** Button click handlers */
  buttons: {
    save: {
      action: 'Call onSave() → await Promise';
      onSuccess: 'Dialog closes, navigation proceeds';
      onError: 'Show error toast, dialog stays open, buttons re-enable';
      loadingState: 'Disable all buttons, show spinner in Save button';
    };

    dontSave: {
      action: 'Call onDiscard()';
      behavior: 'Immediately close dialog, discard form changes, proceed with navigation';
    };

    cancel: {
      action: 'Call onCancel()';
      behavior: 'Close dialog, keep form open, abort navigation';
    };
  };

  /** Keyboard interactions */
  keyboard: {
    Escape: 'Triggers onCancel (abort navigation)';
    Enter: 'Triggers Save button (default action)';
    Tab: 'Focus cycles through Cancel → Don\'t Save → Save';
  };

  /** Focus management */
  focus: {
    onOpen: 'Focus Save button (primary action)';
    onClose: 'Return focus to trigger element (edit icon/button)';
    trapFocus: 'Prevent tabbing outside dialog while open';
  };
}

/**
 * Error handling
 */
export interface UnsavedChangesDialogErrorHandling {
  /** Save operation error scenarios */
  saveErrors: {
    /** Network error (API unreachable) */
    networkError: {
      display: 'Toast notification with error message';
      dialogState: 'Stay open, re-enable buttons';
      userAction: 'Can retry save or choose Don\'t Save/Cancel';
    };

    /** Validation error (shouldn't happen, but possible) */
    validationError: {
      display: 'Toast notification with validation details';
      dialogState: 'Close dialog, return to edit form with errors highlighted';
      userAction: 'Fix validation errors, save again';
    };

    /** Server error (500) */
    serverError: {
      display: 'Toast notification: "Failed to save. Please try again."';
      dialogState: 'Stay open, re-enable buttons';
      userAction: 'Can retry save or contact support';
    };

    /** Entity not found (404) */
    notFoundError: {
      display: 'Toast notification: "Entity no longer exists"';
      dialogState: 'Close dialog, close form, return to empty panel';
      userAction: 'N/A (entity deleted by another process)';
    };
  };

  /** Promise rejection handling */
  catchBlock: `
    try {
      setIsSaving(true);
      await onSave();
      // Success: dialog closes via parent component
    } catch (error) {
      setIsSaving(false);
      toast.error(getErrorMessage(error));
      // Dialog stays open for retry
    }
  `;
}

/**
 * Accessibility requirements (WCAG 2.2 Level AA)
 */
export interface UnsavedChangesDialogAccessibility {
  /** Dialog semantics */
  role: 'dialog' /* Provided by Radix UI DialogPrimitive */;
  ariaModal: true;
  ariaLabelledby: 'Dialog title ID';
  ariaDescribedby: 'Dialog description ID';

  /** Focus management */
  focusTrap: 'Enabled (Radix UI default behavior)';
  initialFocus: 'Save button (primary action)';
  returnFocus: 'Trigger element after close';

  /** Keyboard navigation */
  closeWithEscape: true;
  tabCycling: 'Cancel → Don\'t Save → Save → Cancel (loop)';

  /** Button accessibility */
  buttons: {
    accessibleLabels: 'Clear button text (Save, Don\'t Save, Cancel)';
    disabledState: 'aria-disabled=true when isSaving';
    loadingState: 'aria-busy=true on Save button when isSaving';
  };

  /** Screen reader announcements */
  announcements: {
    dialogOpened: 'Title + description announced automatically';
    savingState: 'Save button text changes to "Saving..." (announced)';
    errorState: 'Toast error announced via aria-live region';
  };

  /** Color contrast */
  textContrast: '4.5:1' /* Dialog text on background */;
  buttonContrast: '3:1' /* Button borders and states */;

  /** Minimum touch targets */
  buttonSize: '44x44px minimum for all buttons';
}

/**
 * Integration with parent components
 */
export interface UnsavedChangesDialogIntegration {
  /** Parent: WorldEntityForm or MainPanel navigation logic */
  usage: `
    const [showDialog, setShowDialog] = useState(false);
    const [pendingAction, setPendingAction] = useState<() => void | null>(null);

    const handleNavigationAttempt = (action: () => void) => {
      if (hasUnsavedChanges) {
        setPendingAction(() => action);
        setShowDialog(true);
      } else {
        action(); // No unsaved changes, proceed immediately
      }
    };

    const handleSave = async () => {
      await saveEntity(); // Throws on error
      pendingAction?.(); // Proceed with navigation
      setShowDialog(false);
    };

    const handleDiscard = () => {
      dispatch(setUnsavedChanges(false));
      pendingAction?.(); // Proceed with navigation
      setShowDialog(false);
    };

    const handleCancel = () => {
      setPendingAction(null);
      setShowDialog(false);
    };
  `;

  /** Redux integration */
  redux: {
    state: 'hasUnsavedChanges from selectHasUnsavedChanges';
    actions: 'setUnsavedChanges(false) on discard';
  };
}

/**
 * Styling and visual design
 */
export interface UnsavedChangesDialogStyling {
  /** Dialog overlay */
  overlay: 'Semi-transparent backdrop (Shadcn/ui default)';

  /** Dialog content */
  content: {
    maxWidth: 'max-w-md' /* Compact dialog */;
    padding: 'p-6';
    borderRadius: 'rounded-lg';
    boxShadow: 'lg';
  };

  /** Title styling */
  title: {
    fontSize: 'text-lg font-semibold';
    marginBottom: 'mb-2';
  };

  /** Description styling */
  description: {
    fontSize: 'text-sm text-muted-foreground';
    marginBottom: 'mb-6';
  };

  /** Footer button group */
  footer: {
    layout: 'flex justify-end gap-2';
    buttons: {
      Cancel: 'variant=outline';
      DontSave: 'variant=secondary';
      Save: 'variant=default (primary)';
    };
  };

  /** Loading state (Save button) */
  loadingButton: {
    icon: '<Loader2 className="animate-spin" />';
    text: 'Saving...';
    disabled: true;
  };
}

/**
 * Test scenarios (for UnsavedChangesDialog.test.tsx)
 */
export interface UnsavedChangesDialogTestScenarios {
  /** Rendering tests */
  rendering: {
    'Renders dialog when open=true': boolean;
    'Does not render when open=false': boolean;
    'Displays correct title and description': boolean;
    'Renders three buttons: Cancel, Don\'t Save, Save': boolean;
    'Shows loading state in Save button when isSaving=true': boolean;
  };

  /** Interaction tests */
  interaction: {
    'Calls onSave when Save button clicked': boolean;
    'Calls onDiscard when Don\'t Save button clicked': boolean;
    'Calls onCancel when Cancel button clicked': boolean;
    'Calls onCancel when Escape key pressed': boolean;
    'Calls onCancel when clicking outside dialog': boolean;
    'Disables all buttons when isSaving=true': boolean;
  };

  /** Async behavior tests */
  async: {
    'Handles successful save (Promise resolves)': boolean;
    'Handles failed save (Promise rejects)': boolean;
    'Shows loading state during async save': boolean;
    'Re-enables buttons after save error': boolean;
  };

  /** Accessibility tests */
  accessibility: {
    'Has no accessibility violations (jest-axe)': boolean;
    'Dialog has role=dialog': boolean;
    'Dialog has aria-modal=true': boolean;
    'Dialog has aria-labelledby pointing to title': boolean;
    'Dialog has aria-describedby pointing to description': boolean;
    'Focus trapped within dialog': boolean;
    'Focus returns to trigger on close': boolean;
    'All buttons keyboard-accessible': boolean;
    'Buttons meet minimum touch target size (44x44px)': boolean;
  };

  /** Edge cases */
  edgeCases: {
    'Handles rapid Save button clicks (debounce/disable)': boolean;
    'Handles save error with custom error message': boolean;
    'Handles onSave returning null/undefined instead of Promise': boolean;
  };
}

/**
 * Usage example
 */
export const UnsavedChangesDialogUsageExample = `
import { UnsavedChangesDialog } from '@/components/MainPanel/UnsavedChangesDialog';
import { useState } from 'react';

export function WorldEntityForm() {
  const [showDialog, setShowDialog] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await updateEntity({ /* ... */ });
      setShowDialog(false);
      proceedWithNavigation();
    } catch (error) {
      toast.error('Failed to save entity');
      // Dialog stays open for retry
    } finally {
      setIsSaving(false);
    }
  };

  const handleDiscard = () => {
    dispatch(setUnsavedChanges(false));
    setShowDialog(false);
    proceedWithNavigation();
  };

  const handleCancel = () => {
    setShowDialog(false);
  };

  return (
    <>
      {/* Form content */}
      <UnsavedChangesDialog
        open={showDialog}
        onSave={handleSave}
        onDiscard={handleDiscard}
        onCancel={handleCancel}
        isSaving={isSaving}
      />
    </>
  );
}
`;
