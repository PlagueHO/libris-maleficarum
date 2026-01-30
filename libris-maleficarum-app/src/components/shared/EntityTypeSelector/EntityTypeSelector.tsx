import React, { useState, useMemo } from 'react';
import {
  ChevronRight,
  Search,
  X,
  Star,
  Globe,
  Map,
  MapPin,
  Building,
  Home,
  User,
  Users,
  Calendar,
  Scroll,
  Package,
  FolderOpen,
  Folder,
  CalendarDays,
  BookOpen,
  BookMarked,
  Bug,
  Box,
  Compass,
  Mountain,
  Shield,
  HelpCircle,
  type LucideIcon,
} from 'lucide-react';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '../../ui/popover';
import { Input } from '../../ui/input';
import { Button } from '../../ui/button';
import { Separator } from '../../ui/separator';
import {
  WorldEntityType,
  getEntityTypeSuggestions,
  getEntityTypeMeta,
} from '@/services/types/worldEntity.types';

/**
 * Icon mapping for entity types
 * Maps icon string names from entityTypeRegistry to Lucide icon components
 */
const iconMap: Record<string, LucideIcon> = {
  Globe,
  Map,
  MapPin,
  Building,
  Home,
  User,
  Users,
  Calendar,
  Scroll,
  Package,
  Folder,
  FolderOpen,
  CalendarDays,
  BookOpen,
  BookMarked,
  Bug,
  Box,
  Compass,
  Mountain,
  Shield,
  HelpCircle,
};

/**
 * Get the icon component for an entity type
 */
function getEntityIcon(iconName: string): LucideIcon | null {
  return iconMap[iconName] || null;
}

export interface EntityTypeSelectorProps {
  /** Currently selected entity type */
  value: WorldEntityType | '';

  /** Called when user selects a type */
  onValueChange: (type: WorldEntityType) => void;

  /** Parent entity type for context-aware suggestions */
  parentType?: WorldEntityType | null;

  /** Whether to allow all types (bypass parent-based filtering) */
  allowAllTypes?: boolean;

  /** Whether the selector is disabled */
  disabled?: boolean;

  /** Custom placeholder text */
  placeholder?: string;

  /** Aria label for accessibility */
  'aria-label'?: string;

  /** Aria invalid state */
  'aria-invalid'?: boolean;
}

/**
 * Advanced entity type selector component
 *
 * Features:
 * - Recommended types at the top (based on parent entity type)
 * - Search functionality to filter by name or description
 * - Types grouped by category (Geography, Characters, Events, etc.)
 * - Accessible with keyboard navigation and screen reader support
 */
export function EntityTypeSelector({
  value,
  onValueChange,
  parentType,
  allowAllTypes = false,
  disabled = false,
  placeholder = 'Select entity type',
  'aria-label': ariaLabel,
  'aria-invalid': ariaInvalid,
}: EntityTypeSelectorProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');

  // Get recommended types (or all types if allowAllTypes is true)
  const recommendedTypes = useMemo(() => {
    if (allowAllTypes) {
      return Object.values(WorldEntityType);
    }
    return getEntityTypeSuggestions(parentType ?? null);
  }, [parentType, allowAllTypes]);

  // Get all entity types
  const allTypes = useMemo(() => {
    return Object.values(WorldEntityType);
  }, []);

  // Filter and organize types
  const { recommendedFiltered, otherFiltered } = useMemo(() => {
    const searchLower = search.toLowerCase();

    // Filter recommended types
    const recommendedFiltered = recommendedTypes.filter((type) => {
      const meta = getEntityTypeMeta(type);
      return (
        meta.label.toLowerCase().includes(searchLower) ||
        meta.description.toLowerCase().includes(searchLower)
      );
    });

    // Get all other types (not in recommended) and always show them
    const otherTypes = allTypes.filter(
      (type) => !recommendedTypes.includes(type)
    );

    // Filter other types (always shown, filtered by search if present)
    const otherFiltered = otherTypes
      .filter((type) => {
        const meta = getEntityTypeMeta(type);
        return (
          meta.label.toLowerCase().includes(searchLower) ||
          meta.description.toLowerCase().includes(searchLower)
        );
      })
      .sort((a, b) => {
        // Sort alphabetically by label
        const metaA = getEntityTypeMeta(a);
        const metaB = getEntityTypeMeta(b);
        return metaA.label.localeCompare(metaB.label);
      });

    return { recommendedFiltered, otherFiltered };
  }, [search, recommendedTypes, allTypes]);

  const selectedMeta = value ? getEntityTypeMeta(value as WorldEntityType) : null;

  const handleSelect = (type: WorldEntityType) => {
    onValueChange(type);
    setOpen(false);
    setSearch('');
  };

  const handleClear = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    // Cast empty string to WorldEntityType for the unselected state
    onValueChange('' as WorldEntityType);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          className="w-full justify-between"
          role="combobox"
          aria-expanded={open}
          aria-label={ariaLabel || placeholder}
          aria-invalid={ariaInvalid}
        >
          <span className="truncate">
            {selectedMeta ? selectedMeta.label : placeholder}
          </span>
          <div className="flex items-center gap-1">
            {value && !disabled && (
              <X
                className="h-4 w-4 flex-shrink-0 opacity-50 hover:opacity-100"
                onClick={handleClear}
              />
            )}
            <ChevronRight className="h-4 w-4 flex-shrink-0 opacity-50" />
          </div>
        </Button>
      </PopoverTrigger>

      <PopoverContent
        className="w-[var(--radix-popover-trigger-width)] p-0"
        align="start"
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <div className="flex flex-col gap-3 p-4">
          {/* Search Input */}
          <div className="relative">
            <Search className="absolute left-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder="Filter..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-8"
              autoFocus
            />
          </div>

          {/* Types List */}
          <div className="max-h-96 overflow-y-auto">
            {/* Recommended Section */}
            {recommendedFiltered.length > 0 && (
              <div className="mb-2 bg-accent/5 -mx-1 px-1">
                <div className="px-2 py-1.5 text-xs font-semibold text-primary uppercase tracking-wide flex items-center gap-1.5">
                  <Star className="w-3.5 h-3.5" aria-hidden="true" />
                  Recommended
                </div>
                <div className="space-y-1 pb-2">
                  {recommendedFiltered.map((type) => {
                    const meta = getEntityTypeMeta(type);
                    const isSelected = value === type;
                    const IconComponent = getEntityIcon(meta.icon);
                    
                    return (
                      <button
                        key={type}
                        onClick={() => handleSelect(type)}
                        className={`w-full text-left px-3 py-2 rounded-md text-sm transition-colors flex items-start gap-2 ${
                          isSelected
                            ? 'bg-primary text-primary-foreground font-medium'
                            : 'hover:bg-accent text-foreground'
                        }`}
                        role="option"
                        aria-selected={isSelected}
                      >
                        {IconComponent && (
                          <IconComponent className="w-4 h-4 flex-shrink-0 mt-0.5" aria-hidden="true" />
                        )}
                        <div className="flex-1">
                          <div className="font-medium text-sm">{meta.label}</div>
                          <div
                            className={`text-xs leading-tight ${
                              isSelected
                                ? 'opacity-90'
                                : 'text-muted-foreground'
                            }`}
                          >
                            {meta.description}
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Other Types (Alphabetically Sorted) */}
            {otherFiltered.length > 0 && (
              <>
                {/* Separator and heading only when there are recommendations */}
                {recommendedFiltered.length > 0 && (
                  <>
                    <Separator className="my-2" decorative={false} />
                    <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                      Other
                    </div>
                  </>
                )}
                
                <div className="space-y-1">
                    {otherFiltered.map((type) => {
                      const meta = getEntityTypeMeta(type);
                      const isSelected = value === type;
                      const IconComponent = getEntityIcon(meta.icon);
                      
                      return (
                        <button
                          key={type}
                          onClick={() => handleSelect(type)}
                          className={`w-full text-left px-3 py-2 rounded-md text-sm transition-colors flex items-start gap-2 ${
                            isSelected
                              ? 'bg-primary text-primary-foreground font-medium'
                              : 'hover:bg-accent text-foreground'
                          }`}
                          role="option"
                          aria-selected={isSelected}
                        >
                          {IconComponent && (
                            <IconComponent className="w-4 h-4 flex-shrink-0 mt-0.5" aria-hidden="true" />
                          )}
                          <div className="flex-1">
                            <div className="font-medium text-sm">{meta.label}</div>
                            <div
                              className={`text-xs leading-tight ${
                                isSelected
                                  ? 'opacity-90'
                                  : 'text-muted-foreground'
                              }`}
                            >
                              {meta.description}
                            </div>
                          </div>
                        </button>
                      );
                    })}
                  </div>
              </>
            )}

            {/* No Results */}
            {recommendedFiltered.length === 0 && otherFiltered.length === 0 && (
              <div className="px-3 py-8 text-center text-sm text-muted-foreground">
                No entity types match '{search}'
              </div>
            )}
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
