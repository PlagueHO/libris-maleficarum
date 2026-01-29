import React, { useState, useMemo } from 'react';
import { ChevronRight, Search, X } from 'lucide-react';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '../../ui/popover';
import { Input } from '../../ui/input';
import { Button } from '../../ui/button';
import {
  WorldEntityType,
  getEntityTypeSuggestions,
  getEntityTypeMeta,
} from '@/services/types/worldEntity.types';

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
  const { recommendedFiltered, otherFiltered, categorized } = useMemo(() => {
    const searchLower = search.toLowerCase();

    // Filter recommended types
    const recommendedFiltered = recommendedTypes.filter((type) => {
      const meta = getEntityTypeMeta(type);
      return (
        meta.label.toLowerCase().includes(searchLower) ||
        meta.description.toLowerCase().includes(searchLower)
      );
    });

    // If there are recommended types and no search, only show recommended
    // If searching, show both recommended and other matching types
    const showOnlyRecommendedMode = recommendedTypes.length > 0 && !search;

    // Get all other types (not in recommended)
    const otherTypes = allTypes.filter(
      (type) => !recommendedTypes.includes(type)
    );

    // Filter other types (only shown when searching or no recommended types available)
    const otherFiltered = !showOnlyRecommendedMode
      ? otherTypes.filter((type) => {
          const meta = getEntityTypeMeta(type);
          return (
            meta.label.toLowerCase().includes(searchLower) ||
            meta.description.toLowerCase().includes(searchLower)
          );
        })
      : [];

    // Categorize the "other" types for better organization
    const categorized = otherFiltered.reduce(
      (acc, type) => {
        const meta = getEntityTypeMeta(type);
        if (!acc[meta.category]) {
          acc[meta.category] = [];
        }
        acc[meta.category].push(type);
        return acc;
      },
      {} as Record<string, WorldEntityType[]>
    );

    return { recommendedFiltered, otherFiltered, categorized };
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
              placeholder="Search entity types..."
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
              <div className="mb-4 pb-4 border-b border-border bg-accent/5 -mx-1 px-1">
                <div className="px-2 py-1.5 text-xs font-semibold text-primary uppercase tracking-wide flex items-center gap-1.5">
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 16 16"
                    fill="currentColor"
                    className="w-3.5 h-3.5"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M8 1.75a.75.75 0 0 1 .692.462l1.41 3.393 3.664.293a.75.75 0 0 1 .428 1.317l-2.791 2.39.853 3.575a.75.75 0 0 1-1.12.814L7.998 12.08l-3.135 1.915a.75.75 0 0 1-1.12-.814l.852-3.574-2.79-2.39a.75.75 0 0 1 .427-1.318l3.663-.293 1.41-3.393A.75.75 0 0 1 8 1.75Z"
                      clipRule="evenodd"
                    />
                  </svg>
                  Recommended
                </div>
                <div className="space-y-1">
                  {recommendedFiltered.map((type) => {
                    const meta = getEntityTypeMeta(type);
                    const isSelected = value === type;
                    return (
                      <button
                        key={type}
                        onClick={() => handleSelect(type)}
                        className={`w-full text-left px-3 py-2.5 rounded-md text-sm transition-colors ${
                          isSelected
                            ? 'bg-primary text-primary-foreground font-medium'
                            : 'hover:bg-accent text-foreground'
                        }`}
                        role="option"
                        aria-selected={isSelected}
                      >
                        <div className="font-medium">{meta.label}</div>
                        <div
                          className={`text-xs ${
                            isSelected
                              ? 'opacity-90'
                              : 'text-muted-foreground'
                          }`}
                        >
                          {meta.description}
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Other Types by Category */}
            {otherFiltered.length > 0 && (
              <div className="space-y-4">
                {Object.entries(categorized)
                  .sort(([catA], [catB]) => catA.localeCompare(catB))
                  .map(([category, types]) => (
                    <div key={category}>
                      <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                        {category}
                      </div>
                      <div className="space-y-1">
                        {types.map((type) => {
                          const meta = getEntityTypeMeta(type);
                          const isSelected = value === type;
                          return (
                            <button
                              key={type}
                              onClick={() => handleSelect(type)}
                              className={`w-full text-left px-3 py-2.5 rounded-md text-sm transition-colors ${
                                isSelected
                                  ? 'bg-primary text-primary-foreground font-medium'
                                  : 'hover:bg-accent text-foreground'
                              }`}
                              role="option"
                              aria-selected={isSelected}
                            >
                              <div className="font-medium">{meta.label}</div>
                              <div
                                className={`text-xs ${
                                  isSelected
                                    ? 'opacity-90'
                                    : 'text-muted-foreground'
                                }`}
                              >
                                {meta.description}
                              </div>
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  ))}
              </div>
            )}

            {/* No Results */}
            {recommendedFiltered.length === 0 && otherFiltered.length === 0 && (
              <div className="px-3 py-8 text-center text-sm text-muted-foreground">
                No entity types match "{search}"
              </div>
            )}
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
