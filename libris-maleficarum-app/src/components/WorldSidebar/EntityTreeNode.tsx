/**
 * EntityTreeNode Component
 *
 * Individual tree node representing a single world entity.
 * Supports expand/collapse, selection, keyboard navigation, and ARIA tree pattern.
 *
 * @module components/WorldSidebar/EntityTreeNode
 */

import { createElement } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { ChevronRight, ChevronDown, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  selectIsNodeExpanded,
  selectSelectedEntityId,
  toggleNodeExpanded,
  setSelectedEntity,
  openEntityFormCreate,
} from '@/store/worldSidebarSlice';
import { getEntityIcon } from '@/lib/entityIcons';
import type { WorldEntity } from '@/services/types/worldEntity.types';
import { EntityContextMenu } from './EntityContextMenu';
import { cn } from '@/lib/utils';

export interface EntityTreeNodeProps {
  /** Entity data to render */
  entity: WorldEntity;

  /** Depth level in tree (0-indexed) */
  level: number;

  /** Children nodes (rendered recursively) */
  children?: React.ReactNode;
}

/**
 * Entity tree node component
 *
 * @param props - Component props
 * @returns Tree node UI
 */
export function EntityTreeNode({ entity, level, children }: EntityTreeNodeProps) {
  const dispatch = useDispatch();
  const isExpanded = useSelector(selectIsNodeExpanded(entity.id));
  const selectedEntityId = useSelector(selectSelectedEntityId);

  const isSelected = selectedEntityId === entity.id;

  const handleToggleExpand = (e: React.MouseEvent | React.KeyboardEvent) => {
    e.stopPropagation();
    e.preventDefault();
    if (entity.hasChildren) {
      dispatch(toggleNodeExpanded(entity.id));
    }
  };

  const handleSelect = () => {
    dispatch(setSelectedEntity(entity.id));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleSelect();
    }
  };

  const handleExpandKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      handleToggleExpand(e);
    }
  };

  const handleQuickCreate = (e: React.MouseEvent | React.KeyboardEvent) => {
    e.stopPropagation();
    e.preventDefault();
    dispatch(openEntityFormCreate(entity.id));
  };

  const indentStyle = { paddingLeft: `${level * 20}px` };

  return (
    <div className="flex flex-col relative" data-level={level}>
      <EntityContextMenu entity={entity}>
        <div
          role="treeitem"
          aria-level={level + 1} // ARIA level is 1-indexed
          aria-selected={isSelected}
          aria-expanded={entity.hasChildren ? isExpanded : undefined}
          tabIndex={isSelected ? 0 : -1}
          className={cn(
            "group flex items-center gap-2 px-2 py-1.5 cursor-pointer rounded transition-colors select-none outline-none relative z-10",
            "hover:bg-accent",
            "focus-visible:outline-2 focus-visible:outline-ring focus-visible:outline-offset-2",
            isSelected && "bg-accent text-accent-foreground font-medium"
          )}
          style={indentStyle}
          onClick={handleSelect}
          onKeyDown={handleKeyDown}
          data-hovered="true"
          data-entity-id={entity.id}
        >
          {entity.hasChildren ? (
            <Button
              variant="icon-expand"
              size="icon-xs"
              onClick={handleToggleExpand}
              onKeyDown={handleExpandKeyDown}
              aria-label={isExpanded ? `Collapse ${entity.name}` : `Expand ${entity.name}`}
              aria-expanded={isExpanded}
              tabIndex={0}
            >
              {isExpanded ? (
                <ChevronDown size={16} aria-hidden="true" />
              ) : (
                <ChevronRight size={16} aria-hidden="true" />
              )}
            </Button>
          ) : (
            <span className="w-5 shrink-0" />
          )}

          {createElement(getEntityIcon(entity.entityType), {
            size: 16,
            className: 'shrink-0 text-muted-foreground',
            'aria-hidden': 'true',
            role: 'img'
          })}

          <span className="flex-1 text-sm overflow-hidden text-ellipsis whitespace-nowrap">{entity.name}</span>

          <Button
            variant="icon-ghost"
            size="icon-sm"
            onClick={handleQuickCreate}
            aria-label={`Add child to ${entity.name}`}
            tabIndex={-1}
            className="opacity-0 group-hover:opacity-100 focus-visible:opacity-100 transition-opacity w-5 h-5 hover:bg-muted"
          >
            <Plus size={14} aria-hidden="true" />
          </Button>
        </div>
      </EntityContextMenu>

      {entity.hasChildren && isExpanded && children}
    </div>
  );
}
