/**
 * WorldSelector Component
 *
 * Dropdown selector to switch between worlds, or prompts to create first world.
 * Displays single world name when only one exists, dropdown when multiple.
 *
 * @module components/WorldSidebar/WorldSelector
 */

import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Settings, Plus } from 'lucide-react';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { useGetWorldsQuery } from '@/services/worldApi';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  setSelectedWorld,
  openWorldFormCreate,
  openWorldFormEdit,
  selectSelectedWorldId,
} from '@/store/worldSidebarSlice';
import { invalidatePattern } from '@/lib/sessionCache';
import { EmptyState } from './EmptyState';
import styles from './WorldSelector.module.css';

/**
 * World selector component for switching between worlds
 *
 * @returns Selector UI
 */
export function WorldSelector() {
  const dispatch = useDispatch();
  const selectedWorldId = useSelector(selectSelectedWorldId);
  const { data: worlds, isLoading, error } = useGetWorldsQuery();

  // Auto-select first world if none selected
  useEffect(() => {
    if (worlds && worlds.length > 0 && !selectedWorldId) {
      dispatch(setSelectedWorld(worlds[0].id));
    }
  }, [worlds, selectedWorldId, dispatch]);

  const handleCreateWorld = () => {
    dispatch(openWorldFormCreate());
  };

  const handleEditWorld = (worldId: string) => {
    dispatch(openWorldFormEdit(worldId));
  };

  const handleWorldChange = (worldId: string) => {
    // Clear cache for previous world before switching
    if (selectedWorldId && selectedWorldId !== worldId) {
      invalidatePattern(new RegExp(`^sidebar_hierarchy_${selectedWorldId}`));
    }
    dispatch(setSelectedWorld(worldId));
  };

  // Loading state
  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading} role="status" aria-label="Loading worlds">
          <div className={styles.skeleton} />
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>Failed to load worlds</p>
        </div>
      </div>
    );
  }

  // Empty state - no worlds
  if (!worlds || worlds.length === 0) {
    return <EmptyState onCreateWorld={handleCreateWorld} />;
  }

  const selectedWorld = worlds.find((w) => w.id === selectedWorldId);
  const sortedWorlds = [...worlds].sort((a, b) => a.name.localeCompare(b.name));

  // Show dropdown with create button
  return (
    <div className={styles.container}>
      <div className={styles.selectorRow}>
        <Select value={selectedWorldId || ''} onValueChange={handleWorldChange}>
          <SelectTrigger className={styles.selectTrigger} aria-label="Select world">
            <SelectValue placeholder="Select a world">
              {selectedWorld?.name || 'Select a world'}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {sortedWorlds.map((world) => (
              <SelectItem
                key={world.id}
                value={world.id}
                aria-selected={world.id === selectedWorldId}
              >
                {world.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <button
                type="button"
                onClick={handleCreateWorld}
                className={styles.iconButton}
                aria-label="Create new world"
              >
                <Plus size={16} aria-hidden="true" />
              </button>
            </TooltipTrigger>
            <TooltipContent>Create new world</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <button
                type="button"
                onClick={() => selectedWorldId && handleEditWorld(selectedWorldId)}
                className={styles.iconButton}
                aria-label="Edit current world"
                disabled={!selectedWorldId}
              >
                <Settings size={16} aria-hidden="true" />
              </button>
            </TooltipTrigger>
            <TooltipContent>Edit world details</TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
    </div>
  );
}
