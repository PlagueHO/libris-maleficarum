/**
 * FormActions Component
 *
 * Reusable button group for form submission and cancellation.
 * Provides consistent styling, layout, and loading states across all forms.
 *
 * Features:
 * - Standard submit/cancel button layout
 * - Unified loading indicator (Loader2 icon)
 * - Consistent spacing and alignment
 * - Accessibility support
 *
 * @module components/ui/form-actions
 */

import { Loader2 } from 'lucide-react';
import { Button } from './button';

export interface FormActionsProps {
  /** Label for the submit button */
  submitLabel?: string;

  /** Label for the cancel button */
  cancelLabel?: string;

  /** Whether the form is loading/submitting */
  isLoading?: boolean;

  /** Callback when cancel button is clicked */
  onCancel: () => void;

  /** Whether submit button is disabled (independent of isLoading) */
  isSubmitDisabled?: boolean;

  /** Additional CSS classes for the container */
  className?: string;
}

/**
 * Form action buttons component
 *
 * @param props - Component props
 * @returns Button group UI with submit and cancel buttons
 */
export function FormActions({
  submitLabel = 'Submit',
  cancelLabel = 'Cancel',
  isLoading = false,
  onCancel,
  isSubmitDisabled = false,
  className = '',
}: FormActionsProps) {
  const isDisabled = isLoading || isSubmitDisabled;

  return (
    <div className={`flex gap-3 justify-end ${className}`}>
      <Button
        type="button"
        variant="outline"
        onClick={onCancel}
        disabled={isLoading}
        aria-label={cancelLabel}
      >
        {cancelLabel}
      </Button>
      <Button type="submit" disabled={isDisabled} aria-busy={isLoading}>
        {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />}
        {submitLabel}
      </Button>
    </div>
  );
}
