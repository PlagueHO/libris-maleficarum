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
import styles from './EntityTreeNode.module.css';

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
    <div className={styles.nodeContainer} data-level={level}>
      <EntityContextMenu entity={entity}>
        <div
          role="treeitem"
          aria-level={level + 1} // ARIA level is 1-indexed
          aria-selected={isSelected}
          aria-expanded={entity.hasChildren ? isExpanded : undefined}
          tabIndex={isSelected ? 0 : -1}
          className={`${styles.node} ${isSelected ? styles.selected : ''}`}
          style={indentStyle}
          onClick={handleSelect}
          onKeyDown={handleKeyDown}
          data-hovered="true"
          data-entity-id={entity.id}
        >
          {entity.hasChildren ? (
            <button
              type="button"
              className={styles.expandButton}
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
            </button>
          ) : (
            <span className={styles.spacer} />
          )}

          {createElement(getEntityIcon(entity.entityType), {
            size: 16,
            className: styles.icon,
            'aria-hidden': 'true',
            role: 'img'
          })}

          <span className={styles.name}>{entity.name}</span>

          <button
            type="button"
            className={styles.quickAddButton}
            onClick={handleQuickCreate}
            aria-label={`Add child to ${entity.name}`}
            tabIndex={-1} // Skip tab order, accessible via hover or context menu
          >
            <Plus size={14} aria-hidden="true" />
          </button>
        </div>
      </EntityContextMenu>

      {entity.hasChildren && isExpanded && children}
    </div>
  );
}
