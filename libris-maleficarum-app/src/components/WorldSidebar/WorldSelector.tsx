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
import { Skeleton } from '@/components/ui/skeleton';
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
      <div className="p-4 border-b border-border">
        <div className="flex items-center justify-center min-h-10" role="status" aria-label="Loading worlds">
          <Skeleton className="w-full h-9" />
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="p-4 border-b border-border">
        <div className="px-3 py-3 bg-destructive text-destructive-foreground rounded-md text-sm text-center">
          <p className="m-0">Failed to load worlds</p>
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
    <div className="p-4 border-b border-border">
      <div className="flex items-center gap-2">
        <Select value={selectedWorldId || ''} onValueChange={handleWorldChange}>
          <SelectTrigger className="flex-1" aria-label="Select world">
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
                className="flex items-center justify-center p-1.5 bg-transparent border-none rounded text-muted-foreground hover:bg-accent hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring focus-visible:outline-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors min-w-8 min-h-8"
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
                className="flex items-center justify-center p-1.5 bg-transparent border-none rounded text-muted-foreground hover:bg-accent hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring focus-visible:outline-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors min-w-8 min-h-8"
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
