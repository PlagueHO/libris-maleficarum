/**
 * Sonner Toast Wrapper
 *
 * Wraps the Sonner toast library with Shadcn/UI-compatible styling.
 * Provides accessible toast notifications for ephemeral messages
 * (errors, success confirmations, etc.).
 *
 * @module components/ui/sonner
 */

import { Toaster as Sonner, type ToasterProps } from 'sonner';

/**
 * Toaster component for rendering toast notifications.
 *
 * Place once at the app root (e.g., in App.tsx).
 * Then use `toast()`, `toast.error()`, `toast.success()` etc. from `'sonner'`
 * anywhere in the application to trigger a notification.
 *
 * @example
 * ```tsx
 * // In App.tsx
 * import { Toaster } from '@/components/ui/sonner';
 * <Toaster />
 *
 * // In any component
 * import { toast } from 'sonner';
 * toast.error('Something went wrong');
 * ```
 */
function Toaster({ ...props }: ToasterProps) {
  return (
    <Sonner
      className="toaster group"
      toastOptions={{
        classNames: {
          toast:
            'group toast group-[.toaster]:bg-background group-[.toaster]:text-foreground group-[.toaster]:border-border group-[.toaster]:shadow-lg',
          description: 'group-[.toast]:text-muted-foreground',
          actionButton:
            'group-[.toast]:bg-primary group-[.toast]:text-primary-foreground',
          cancelButton:
            'group-[.toast]:bg-muted group-[.toast]:text-muted-foreground',
        },
      }}
      {...props}
    />
  );
}

export { Toaster };
