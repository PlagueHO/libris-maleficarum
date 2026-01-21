/**
 * Skeleton Component
 *
 * Loading skeleton with shimmer animation for placeholder content.
 * Follows Shadcn/UI patterns for reusable UI primitives.
 *
 * @module components/ui/skeleton
 */

import { cn } from '@/lib/utils';

export interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  /**
   * Custom class names to apply
   */
  className?: string;
}

/**
 * Skeleton loading placeholder with shimmer animation
 *
 * @example
 * ```tsx
 * <Skeleton className="h-9 w-full" />
 * <Skeleton className="h-8 w-32" />
 * ```
 */
export function Skeleton({ className, ...props }: SkeletonProps) {
  return (
    <div
      className={cn(
        'bg-gradient-to-r from-muted via-muted-foreground/20 to-muted',
        'bg-[length:200%_100%] animate-[shimmer_1.5s_infinite]',
        'rounded-md opacity-20',
        className
      )}
      {...props}
    />
  );
}
