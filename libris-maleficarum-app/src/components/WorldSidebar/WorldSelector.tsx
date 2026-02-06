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
import { Pencil, Plus } from 'lucide-react';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Button } from '@/components/ui/button';
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
import { logger } from '@/lib/logger';
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
    logger.userAction('Open create world form');
    dispatch(openWorldFormCreate());
  };

  const handleEditWorld = (worldId: string) => {
    logger.userAction('Open edit world form', { worldId });
    dispatch(openWorldFormEdit(worldId));
  };

  const handleWorldChange = (worldId: string) => {
    logger.userAction('Switch world', { worldId });
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
          <p className="m-0">The grimoire could not be opened</p>
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
          <SelectTrigger className="flex-1" aria-label="Choose a realm">
            <SelectValue placeholder="Choose a realm">
              {selectedWorld?.name || 'Choose a realm'}
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
              <Button
                variant="icon-ghost"
                size="icon-sm"
                onClick={() => selectedWorldId && handleEditWorld(selectedWorldId)}
                aria-label="Edit current world"
                disabled={!selectedWorldId}
              >
                <Pencil size={16} aria-hidden="true" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Amend realm details</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="icon-ghost"
                size="icon-sm"
                onClick={handleCreateWorld}
                aria-label="Create new world"
              >
                <Plus size={16} aria-hidden="true" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Create new realm</TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
    </div>
  );
}
