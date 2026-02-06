/**
 * EntityDetailReadOnlyView Component
 *
 * Read-only display of a world entity with Edit button.
 * Displays entity name, type badge, tags, description, and custom properties.
 *
 * @module components/MainPanel/EntityDetailReadOnlyView
 * @see specs/008-edit-world-entity/contracts/EntityDetailReadOnlyView.contract.ts
 */

import { Edit } from 'lucide-react';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import type { WorldEntity } from '@/services/types/worldEntity.types';
import { formatEntityType } from '@/lib/entityTypeHelpers';
import { DynamicPropertiesView } from './DynamicPropertiesView';

export interface EntityDetailReadOnlyViewProps {
  /** Entity to display in read-only mode */
  entity: WorldEntity;

  /** Callback invoked when Edit button is clicked */
  onEditClick: () => void;

  /** Disable Edit button (optional, defaults to false) */
  disableEdit?: boolean;
}

/**
 * EntityDetailReadOnlyView component
 *
 * @param props - Component props
 * @returns Read-only entity detail view UI
 */
export function EntityDetailReadOnlyView({
  entity,
  onEditClick,
  disableEdit = false,
}: EntityDetailReadOnlyViewProps) {
  const hasDescription = entity.description && entity.description.trim().length > 0;
  
  // Parse properties JSON string to object
  let customProperties: Record<string, unknown> | null = null;
  if (entity.properties) {
    try {
      const parsed = typeof entity.properties === 'string'
        ? JSON.parse(entity.properties)
        : entity.properties;
      customProperties = parsed;
    } catch {
      // Ignore parse errors
      customProperties = null;
    }
  }
  
  const hasCustomProperties =
    customProperties &&
    typeof customProperties === 'object' &&
    Object.keys(customProperties).length > 0;

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <Card>
        <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-4">
          <div className="space-y-3">
            <h1 className="text-3xl font-bold">{entity.name}</h1>
            <div className="flex items-center gap-2 flex-wrap">
              <Badge variant="outline">{formatEntityType(entity.entityType)}</Badge>
              {entity.tags &&
                entity.tags.length > 0 &&
                entity.tags.map((tag) => (
                  <Badge key={tag} variant="secondary">
                    {tag}
                  </Badge>
                ))}
            </div>
          </div>

          <Button
            variant="outline"
            size="default"
            onClick={onEditClick}
            disabled={disableEdit}
            aria-label={`Edit ${entity.name}`}
            tabIndex={0}
            className="shrink-0"
          >
            <Edit className="mr-2 h-4 w-4" aria-hidden="true" />
            Edit
          </Button>
        </CardHeader>

        <CardContent className="space-y-6">
          {/* Description */}
          <div>
            <h2 className="text-lg font-semibold mb-2">Description</h2>
            {hasDescription ? (
              <p className="prose dark:prose-invert max-w-none whitespace-pre-wrap">
                {entity.description}
              </p>
            ) : (
              <p className="text-muted-foreground italic">No lore has been inscribed for this entry.</p>
            )}
          </div>

          {/* T035: Custom Properties using DynamicPropertiesView */}
          {hasCustomProperties && customProperties && (
            <DynamicPropertiesView
              entityType={entity.entityType}
              value={customProperties}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
