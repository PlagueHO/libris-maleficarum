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

import { useState, useCallback } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { Plus } from 'lucide-react';
import {
  selectSelectedWorldId,
  openEntityFormCreate,
} from '@/store/worldSidebarSlice';
import { Button } from '@/components/ui/button';
import { WorldSelector } from './WorldSelector';
import { EntityTree } from './EntityTree';
import { OptimisticDeleteProvider } from './OptimisticDeleteContext';

/**
 * World sidebar container component
 *
 * @returns Sidebar UI
 */
export function WorldSidebar() {
  const dispatch = useDispatch();
  const selectedWorldId = useSelector(selectSelectedWorldId);
  
  // Track optimistically deleted entity IDs
  const [optimisticallyDeletedIds, setOptimisticallyDeletedIds] = useState<Set<string>>(new Set());

  const handleAddRootEntity = () => {
    dispatch(openEntityFormCreate(null));
  };
  
  /**
   * Optimistically remove entity from UI before backend delete completes
   * Called by DeleteConfirmationModal when user confirms delete
   */
  const handleOptimisticDelete = useCallback((entityId: string, childIds?: string[]) => {
    setOptimisticallyDeletedIds(prev => {
      const next = new Set(prev);
      next.add(entityId);
      
      // Add child entity IDs for cascading deletes
      if (childIds) {
        childIds.forEach(id => next.add(id));
      }
      
      return next;
    });
  }, []);

  return (
    <OptimisticDeleteProvider value={{ onOptimisticDelete: handleOptimisticDelete }}>
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
    </OptimisticDeleteProvider>
  );
}