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

import type { ReactNode } from 'react';
import { ArrowLeft } from 'lucide-react';

export interface FormLayoutProps {
  /** Form content/children */
  children: ReactNode;

  /** Callback when back button is clicked */
  onBack: () => void;

  /** Optional back button label (defaults to "Back") */
  backLabel?: string;
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
}: FormLayoutProps) {
  return (
    <main className="flex-1 p-6 overflow-auto">
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
    </main>
  );
}
