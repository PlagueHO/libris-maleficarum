/**
 * EmptyState Component
 *
 * Displays a friendly prompt when the user has no worlds yet.
 * Encourages creating their first world to start building a campaign.
 *
 * @module components/WorldSidebar/EmptyState
 */

import { Globe } from 'lucide-react';
import styles from './EmptyState.module.css';

export interface EmptyStateProps {
  /** Callback when "Create World" button is clicked */
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
    <div className={styles.container} role="region" aria-label="Empty world list">
      <div className={styles.content}>
        {/* Decorative icon */}
        <Globe
          className={styles.icon}
          size={64}
          strokeWidth={1.5}
          aria-hidden="true"
        />

        {/* Heading */}
        <h2 className={styles.heading}>Create Your First World</h2>

        {/* Description */}
        <p className={styles.description}>
          Start building your campaign world. Add continents, countries, cities,
          characters, and more to bring your stories to life.
        </p>

        {/* Call-to-action button */}
        <button
          type="button"
          onClick={onCreateWorld}
          className={styles.button}
          aria-label="Create World"
        >
          <Globe size={20} aria-hidden="true" />
          <span>Create World</span>
        </button>
      </div>
    </div>
  );
}
