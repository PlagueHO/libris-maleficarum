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
import {
  selectIsWorldFormOpen,
  selectEditingWorldId,
  closeWorldForm,
} from '@/store/worldSidebarSlice';
import { useGetWorldByIdQuery } from '@/services/worldApi';
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

  // Fetch world data if editing
  const { data: editingWorld } = useGetWorldByIdQuery(editingWorldId!, {
    skip: !editingWorldId,
  });

  const handleCloseWorldForm = () => {
    dispatch(closeWorldForm());
  };

  return (
    <aside className={styles.sidebar} role="complementary" aria-label="World navigation">
      {/* World Selector */}
      <WorldSelector />

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
