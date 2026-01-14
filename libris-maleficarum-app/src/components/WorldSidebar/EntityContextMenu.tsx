/**
 * EntityContextMenu Component
 *
 * Provides right-click actions for World Entities:
 * - Add Child Entity
 * - Edit Entity
 * - Delete Entity
 *
 * @module components/WorldSidebar/EntityContextMenu
 */

import type { PropsWithChildren } from 'react';
import { useDispatch } from 'react-redux';
import { Plus, Edit, Trash2, Move } from 'lucide-react';
import {
  ContextMenu,
  ContextMenuTrigger,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
} from '@/components/ui/context-menu';
import {
  openEntityFormCreate,
  openEntityFormEdit,
} from '@/store/worldSidebarSlice';
import type { WorldEntity } from '@/services/types/worldEntity.types';

export interface EntityContextMenuProps extends PropsWithChildren {
  /** The entity to perform actions on */
  entity: WorldEntity;
}

/**
 * Context menu wrapper for entity items
 *
 * @param props - Component props
 * @returns Context Menu wrapper
 */
export function EntityContextMenu({ entity, children }: EntityContextMenuProps) {
  const dispatch = useDispatch();

  const handleCreateChild = () => {
    dispatch(openEntityFormCreate(entity.id));
  };

  const handleEdit = () => {
    dispatch(openEntityFormEdit(entity.id));
  };

  const handleDelete = () => {
    // TODO: Replace with proper dialog in T111
    if (confirm(`Are you sure you want to delete "${entity.name}"?`)) {
      console.log('Delete confirmed', entity.id);
      // dispatch(deleteEntity(entity.id));
    }
  };

  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>{children}</ContextMenuTrigger>
      <ContextMenuContent className="w-56">
        <ContextMenuItem onClick={handleCreateChild}>
          <Plus className="mr-2 h-4 w-4" />
          Add Child Entity
        </ContextMenuItem>
        <ContextMenuItem onClick={handleEdit}>
          <Edit className="mr-2 h-4 w-4" />
          Edit Entity
        </ContextMenuItem>
        <ContextMenuItem disabled>
          <Move className="mr-2 h-4 w-4" />
          Move Entity
        </ContextMenuItem>
        <ContextMenuSeparator />
        <ContextMenuItem
          onClick={handleDelete}
          className="text-red-600 focus:text-red-600 focus:bg-red-50 dark:focus:bg-red-900/10"
        >
          <Trash2 className="mr-2 h-4 w-4" />
          Delete Entity
        </ContextMenuItem>
      </ContextMenuContent>
    </ContextMenu>
  );
}
