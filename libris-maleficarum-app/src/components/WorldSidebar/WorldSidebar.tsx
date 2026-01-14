/**
 * WorldSidebar Container Component
 *
 * Main sidebar container that orchestrates:
 * - World selection via WorldSelector
 * - Entity tree navigation (future)
 * - World/entity form modals
 *
 * @module components/WorldSidebar/WorldSidebar
 */

import { useSelector, useDispatch } from 'react-redux';
import { Plus } from 'lucide-react';
import {
  selectIsWorldFormOpen,
  selectEditingWorldId,
  selectSelectedWorldId,
  closeWorldForm,
  openEntityFormCreate,
} from '@/store/worldSidebarSlice';
import { useGetWorldByIdQuery } from '@/services/worldApi';
import { Button } from '@/components/ui/button';
import { WorldSelector } from './WorldSelector';
import { WorldFormModal } from './WorldFormModal';
import { EntityFormModal } from './EntityFormModal';
import { EntityTree } from './EntityTree';
import styles from './WorldSidebar.module.css';

/**
 * World sidebar container component
 *
 * @returns Sidebar UI
 */
export function WorldSidebar() {
  const dispatch = useDispatch();
  const isWorldFormOpen = useSelector(selectIsWorldFormOpen);
  const editingWorldId = useSelector(selectEditingWorldId);
  const selectedWorldId = useSelector(selectSelectedWorldId);

  // Fetch world data if editing
  const { data: editingWorld } = useGetWorldByIdQuery(editingWorldId!, {
    skip: !editingWorldId,
  });

  const handleCloseWorldForm = () => {
    dispatch(closeWorldForm());
  };

  const handleAddRootEntity = () => {
    dispatch(openEntityFormCreate(null));
  };

  return (
    <aside className={styles.sidebar} role="complementary" aria-label="World navigation">
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
      <EntityTree />

      {/* World Form Modal */}
      <WorldFormModal
        isOpen={isWorldFormOpen}
        mode={editingWorldId ? 'edit' : 'create'}
        world={editingWorld}
        onClose={handleCloseWorldForm}
      />

      {/* Entity Form Modal */}
      <EntityFormModal />
    </aside>
  );
}
