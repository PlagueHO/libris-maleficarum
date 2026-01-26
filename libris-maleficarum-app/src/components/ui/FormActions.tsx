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

  /** Optional icon component for the submit button */
  submitIcon?: React.ComponentType<{ className?: string; 'aria-hidden'?: boolean }>;

  /** Optional icon component for the cancel button */
  cancelIcon?: React.ComponentType<{ className?: string; 'aria-hidden'?: boolean }>;
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
  submitIcon: SubmitIcon,
  cancelIcon: CancelIcon,
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
        {CancelIcon && <CancelIcon className="mr-2 h-4 w-4" aria-hidden />}
        {cancelLabel}
      </Button>
      <Button type="submit" disabled={isDisabled} aria-busy={isLoading}>
        {isLoading ? (
          <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
        ) : (
          SubmitIcon && <SubmitIcon className="mr-2 h-4 w-4" aria-hidden />
        )}
        {submitLabel}
      </Button>
    </div>
  );
}
