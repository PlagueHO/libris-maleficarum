/**
 * EntityTree Component
 *
 * Recursive tree component for displaying world entity hierarchy.
 * Supports lazy loading, sessionStorage caching, and keyboard navigation.
 *
 * @module components/WorldSidebar/EntityTree
 */

import { useEffect, useRef, useState } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { Plus } from 'lucide-react';
import { useGetEntitiesByParentQuery } from '@/services/worldEntityApi';
import { Skeleton } from '@/components/ui/skeleton';
import {
  selectSelectedWorldId,
  selectExpandedNodeIds,
  selectSelectedEntityId,
  setSelectedEntity,
  toggleNodeExpanded,
  openEntityFormCreate,
} from '@/store/worldSidebarSlice';
import { EntityTreeNode } from './EntityTreeNode';
import { get as cacheGet, set as cacheSet } from '@/lib/sessionCache';
import type { WorldEntity } from '@/services/types/worldEntity.types';

/**
 * Entity tree component
 *
 * @returns Tree UI
 */
export function EntityTree() {
  const selectedWorldId = useSelector(selectSelectedWorldId);
  const selectedEntityId = useSelector(selectSelectedEntityId);
  const expandedNodeIds = useSelector(selectExpandedNodeIds);
  const dispatch = useDispatch();
  const treeRef = useRef<HTMLDivElement>(null);
  const [flattenedEntities, setFlattenedEntities] = useState<WorldEntity[]>([]);

  // Flatten the tree for keyboard navigation
  useEffect(() => {
    if (treeRef.current) {
      const nodes = Array.from(
        treeRef.current.querySelectorAll('[role="treeitem"]'),
      ) as HTMLElement[];
      const entities: WorldEntity[] = [];
      nodes.forEach((node) => {
        const entityId = node.getAttribute('data-entity-id');
        if (entityId) {
          // Store reference for navigation
          entities.push({ id: entityId } as WorldEntity);
        }
      });
      // eslint-disable-next-line react-hooks/set-state-in-effect -- Synchronizing state with DOM tree structure
      setFlattenedEntities(entities);
    }
  }, [expandedNodeIds, selectedWorldId]);

  // Handle keyboard navigation
  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (!selectedEntityId || flattenedEntities.length === 0) return;

    const currentIndex = flattenedEntities.findIndex(
      (entity) => entity.id === selectedEntityId,
    );
    if (currentIndex === -1) return;

    let handled = false;

    switch (e.key) {
      case 'ArrowDown':
        // Move to next item
        if (currentIndex < flattenedEntities.length - 1) {
          const nextEntity = flattenedEntities[currentIndex + 1];
          dispatch(setSelectedEntity(nextEntity.id));
          handled = true;
        }
        break;

      case 'ArrowUp':
        // Move to previous item
        if (currentIndex > 0) {
          const prevEntity = flattenedEntities[currentIndex - 1];
          dispatch(setSelectedEntity(prevEntity.id));
          handled = true;
        }
        break;

      case 'ArrowRight':
        // Expand node if collapsed and has children
        if (treeRef.current) {
          const currentNode = treeRef.current.querySelector(
            `[data-entity-id="${selectedEntityId}"]`,
          );
          if (currentNode) {
            const isExpanded = currentNode.getAttribute('aria-expanded') === 'true';
            const hasChildren = currentNode.hasAttribute('aria-expanded');
            if (hasChildren && !isExpanded) {
              dispatch(toggleNodeExpanded(selectedEntityId));
              handled = true;
            }
          }
        }
        break;

      case 'ArrowLeft':
        // Collapse node if expanded
        if (treeRef.current) {
          const currentNode = treeRef.current.querySelector(
            `[data-entity-id="${selectedEntityId}"]`,
          );
          if (currentNode) {
            const isExpanded = currentNode.getAttribute('aria-expanded') === 'true';
            if (isExpanded) {
              dispatch(toggleNodeExpanded(selectedEntityId));
              handled = true;
            }
          }
        }
        break;

      case 'Enter':
      case ' ':
        // Already handled by node's onClick
        handled = true;
        break;
    }

    if (handled) {
      e.preventDefault();
      e.stopPropagation();
    }
  };

  if (!selectedWorldId) {
    return (
      <div className="px-4 py-8 text-center text-muted-foreground">
        <p className="m-0 text-sm">Select a world to view entities</p>
      </div>
    );
  }

  return (
    <div ref={treeRef} onKeyDown={handleKeyDown}>
      <EntityTreeLevel parentId={null} worldId={selectedWorldId} level={0} />
    </div>
  );
}

interface EntityTreeLevelProps {
  parentId: string | null;
  worldId: string;
  level: number;
}

/**
 * Recursive tree level component
 * Fetches children for a given parent entity
 */
function EntityTreeLevel({ parentId, worldId, level }: EntityTreeLevelProps) {
  const dispatch = useDispatch();
  const expandedNodeIds = useSelector(selectExpandedNodeIds);
  const cacheKey = `sidebar_hierarchy_${worldId}_${parentId || 'root'}`;

  // Check cache first
  const cachedData = cacheGet<WorldEntity[]>(cacheKey, []);

  const {
    data: fetchedEntities,
    isLoading,
    error,
  } = useGetEntitiesByParentQuery(
    { worldId, parentId },
    {
      // Always fetch to ensure fresh data after mutations
      // RTK Query cache tags will invalidate when entities are created/updated/deleted
      refetchOnMountOrArgChange: true, // Force refetch on arg change
    },
  );

  // Use fetched data if available, otherwise use cache as fallback
  const entities = fetchedEntities || cachedData;

  // Update cache when data fetched (keep sync)
  useEffect(() => {
    if (fetchedEntities && fetchedEntities.length > 0) {
      cacheSet(cacheKey, fetchedEntities);
    }
  }, [fetchedEntities, cacheKey]);

  // Loading state
  if (isLoading) {
    return (
      <div className="p-4 flex flex-col gap-2" role="status" aria-label="Loading entities">
        <Skeleton className="w-full h-8" />
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="p-4 text-center text-destructive">
        <p className="m-0 text-sm">Failed to load entities</p>
      </div>
    );
  }

  // Empty state
  if (!entities || entities.length === 0) {
    if (parentId === null) {
      // Root level empty
      return (
        <div className="px-4 py-8 text-center text-muted-foreground">
          <p className="m-0 mb-2 text-sm">No entities yet</p>
          <button
            type="button"
            onClick={() => dispatch(openEntityFormCreate(null))}
            className="inline-flex items-center gap-2 px-4 py-2 rounded-md border border-border bg-background text-foreground cursor-pointer text-sm hover:bg-accent hover:border-accent focus:outline-2 focus:outline-ring focus:outline-offset-2 transition-all"
            aria-label="Add root entity"
          >
            <Plus size={16} aria-hidden="true" />
            Add Root Entity
          </button>
        </div>
      );
    }
    return null; // Child level empty - don't show anything
  }

  // Render tree
  if (level === 0) {
    // Root level - add tree role
    return (
      <div role="tree" aria-label="Entity hierarchy" className="flex-1 overflow-y-auto p-2">
        {entities.map((entity) => (
          <EntityTreeNode key={entity.id} entity={entity} level={level}>
            {entity.hasChildren && expandedNodeIds.includes(entity.id) && (
              <EntityTreeLevel
                parentId={entity.id}
                worldId={worldId}
                level={level + 1}
              />
            )}
          </EntityTreeNode>
        ))}
      </div>
    );
  }

  // Child levels - no role needed
  return (
    <>
      {entities.map((entity) => (
        <EntityTreeNode key={entity.id} entity={entity} level={level}>
          {entity.hasChildren && expandedNodeIds.includes(entity.id) && (
            <EntityTreeLevel
              parentId={entity.id}
              worldId={worldId}
              level={level + 1}
            />
          )}
        </EntityTreeNode>
      ))}
    </>
  );
}
