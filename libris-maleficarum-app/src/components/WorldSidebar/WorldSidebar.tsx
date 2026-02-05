/**
 * WorldSidebar Container Component
 *
 * Main sidebar container that orchestrates:
 * - World selection via WorldSelector
 * - Entity tree navigation (future)
 * - Entity form modals
 * - Optimistic UI updates for async operations
 *
 * @module components/WorldSidebar/WorldSidebar
 */

import { useSelector, useDispatch } from 'react-redux';
import { Plus } from 'lucide-react';
import {
  selectSelectedWorldId,
  openEntityFormCreate,
} from '@/store/worldSidebarSlice';
import { Button } from '@/components/ui/button';
import { WorldSelector } from './WorldSelector';
import { EntityTree } from './EntityTree';

export interface WorldSidebarProps {
  /**
   * Set of entity IDs that have been optimistically deleted
   */
  optimisticallyDeletedIds: Set<string>;
}

/**
 * World sidebar container component
 *
 * @returns Sidebar UI
 */
export function WorldSidebar({ optimisticallyDeletedIds }: WorldSidebarProps) {
  const dispatch = useDispatch();
  const selectedWorldId = useSelector(selectSelectedWorldId);

  const handleAddRootEntity = () => {
    dispatch(openEntityFormCreate(null));
  };

  return (
    <aside data-testid="world-sidebar" className="flex flex-col w-80 h-screen bg-background border-r border-border overflow-hidden" role="complementary" aria-label="World navigation">
      {/* World Selector */}
      <WorldSelector />

      {/* Entity Toolbar */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-border/40">
        <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
          Entities
        </span>
        <Button
          variant="ghost"
          size="icon"
          className="h-6 w-6"
          onClick={handleAddRootEntity}
          disabled={!selectedWorldId}
          aria-label="Add Root Entity"
        >
          <Plus className="h-4 w-4" />
        </Button>
      </div>

      {/* Entity Tree Navigation */}
      <EntityTree optimisticallyDeletedIds={optimisticallyDeletedIds} />
    </aside>
  );
}