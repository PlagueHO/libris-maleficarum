/**
 * EmptyState Component
 *
 * Displays a friendly prompt when the user has no worlds yet.
 * Encourages creating their first world to start building a campaign.
 *
 * @module components/WorldSidebar/EmptyState
 */

import { Globe } from 'lucide-react';
import { Button } from '@/components/ui/button';

export interface EmptyStateProps {
  /** Callback when "Create Realm" button is clicked */
  onCreateWorld: () => void;
}

/**
 * Empty state component shown when user has no worlds
 *
 * @param props - Component props
 * @returns Empty state UI
 */
export function EmptyState({ onCreateWorld }: EmptyStateProps) {
  return (
    <div 
      className="flex items-center justify-center min-h-[300px] px-6 py-8"
      role="region" 
      aria-label="Empty world list"
    >
      <div className="flex flex-col items-center text-center max-w-xs">
        {/* Decorative icon */}
        <Globe
          className="text-muted-foreground opacity-50 mb-6"
          size={64}
          strokeWidth={1.5}
          aria-hidden="true"
        />

        {/* Heading */}
        <h2 className="text-xl font-semibold mb-3">Forge Your First Realm</h2>

        {/* Description */}
        <p className="text-sm text-muted-foreground leading-normal mb-6">
          Every great saga begins with a single realm. Inscribe its continents,
          kingdoms, cities, and denizens to bring your legends to life.
        </p>

        {/* Call-to-action button */}
        <Button
          onClick={onCreateWorld}
          aria-label="Create Realm"
          className="gap-2"
        >
          <Globe size={20} aria-hidden="true" />
          <span>Create Realm</span>
        </Button>
      </div>
    </div>
  );
}
