/**
 * FormLayout Component
 *
 * Reusable layout wrapper for main panel forms.
 * Provides consistent structure, spacing, and navigation for all form pages.
 *
 * Features:
 * - Main content area with proper padding and overflow handling
 * - Consistent max-width container
 * - Back button with navigation
 * - Accessibility support
 *
 * @module components/ui/form-layout
 */

import type { ComponentPropsWithoutRef, ReactNode } from 'react';
import { ArrowLeft } from 'lucide-react';

export interface FormLayoutProps extends ComponentPropsWithoutRef<'main'> {
  /** Form content/children */
  children: ReactNode;

  /** Callback when back button is clicked */
  onBack: () => void;

  /** Optional back button label (defaults to "Back") */
  backLabel?: string;

  /** Optional footer content rendered at the bottom of the visible panel */
  footer?: ReactNode;
}

/**
 * Form layout component for main panel forms
 *
 * @param props - Component props
 * @returns Form container UI
 */
export function FormLayout({
  children,
  onBack,
  backLabel = 'Back',
  footer,
  className,
  ...mainProps
}: FormLayoutProps) {
  return (
    <main
      className={`flex flex-1 min-h-0 flex-col ${className ?? ''}`.trim()}
      {...mainProps}
    >
      <div className="flex-1 overflow-auto p-6">
        <div className="max-w-4xl mx-auto">
          <button
            onClick={onBack}
            className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground mb-6 transition-colors"
            aria-label={backLabel}
          >
            <ArrowLeft className="h-4 w-4" />
            {backLabel}
          </button>

          {children}
        </div>
      </div>

      {footer && (
        <div className="border-t bg-background/95 px-6 py-4 backdrop-blur supports-backdrop-filter:bg-background/80">
          {footer}
        </div>
      )}
    </main>
  );
}
