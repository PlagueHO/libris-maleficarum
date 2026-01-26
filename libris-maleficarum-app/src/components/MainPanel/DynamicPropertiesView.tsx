/**
 * DynamicPropertiesView Component
 *
 * Schema-driven dynamic property renderer for read-only entity display.
 * Renders formatted property values based on the entity type's propertySchema from the registry.
 *
 * Features:
 * - Automatic formatted display from schema
 * - Section header with entity type-specific title
 * - Numeric formatting with thousand separators
 * - TagArray rendering as badges
 * - Fallback to generic Object.entries() when schema missing
 *
 * @module components/MainPanel/DynamicPropertiesView
 */

import type { WorldEntityType } from '@/services/types/worldEntity.types';
import { getEntityTypeConfig } from '@/services/config/entityTypeRegistry';
import { Badge } from '@/components/ui/badge';

/**
 * Props for the DynamicPropertiesView component
 *
 * @public
 */
export interface DynamicPropertiesViewProps {
  /**
   * Entity type to render properties for
   * @required
   */
  entityType: WorldEntityType;

  /**
   * Current property values object
   * - Shape determined by entity type's propertySchema
   * - Keys match schema field keys
   * @required
   */
  value: Record<string, unknown> | null;
}

/**
 * Format numeric value with thousand separators
 *
 * @param value - Numeric value to format
 * @returns Formatted string with thousand separators (e.g., "1,234,567.89")
 * @internal
 */
function formatNumericValue(value: number): string {
  // T033: Format with thousand separators and preserve decimals
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: 10, // Preserve all decimal places
  }).format(value);
}

/**
 * Render formatted field value based on schema type
 *
 * @param fieldKey - Schema field key
 * @param fieldValue - Raw field value
 * @param fieldType - Schema field type
 * @returns Formatted JSX element
 * @internal
 */
function renderFormattedValue(
  fieldKey: string,
  fieldValue: unknown,
  fieldType: string
): React.ReactNode {
  // Handle null/undefined
  if (fieldValue === null || fieldValue === undefined) {
    return <span className="text-muted-foreground italic">Not set</span>;
  }

  // T033: Numeric formatting with thousand separators
  if (fieldType === 'integer' || fieldType === 'decimal') {
    if (typeof fieldValue === 'number') {
      return formatNumericValue(fieldValue);
    }
    // Fallback for non-numeric values
    return String(fieldValue);
  }

  // T034: TagArray rendering as badges
  if (fieldType === 'tagArray') {
    if (Array.isArray(fieldValue) && fieldValue.length > 0) {
      return (
        <div className="flex flex-wrap gap-2">
          {fieldValue.map((tag, index) => (
            <Badge key={`${fieldKey}-${index}`} variant="secondary">
              {String(tag)}
            </Badge>
          ))}
        </div>
      );
    }
    return <span className="text-muted-foreground italic">No items</span>;
  }

  // Text/textarea: render as-is with multiline support
  if (fieldType === 'text' || fieldType === 'textarea') {
    const textValue = String(fieldValue);
    return (
      <span className="whitespace-pre-wrap">
        {textValue}
      </span>
    );
  }

  // Fallback: convert to string
  return String(fieldValue);
}

/**
 * DynamicPropertiesView - Schema-driven read-only property renderer
 *
 * Dynamically generates a formatted property display section based on the
 * entity type's propertySchema from the registry. If no schema exists,
 * falls back to generic Object.entries() renderer.
 *
 * **Behavior**:
 * - If entity type has propertySchema: renders fields in schema order with formatting
 * - If entity type has no schema: falls back to Object.entries() renderer (FR-008)
 * - Section header format: `"{Entity Type Label} Properties"` (e.g., "Geographic Region Properties")
 * - Numeric values: formatted with thousand separators (T033)
 * - TagArray values: rendered as badge chips (T034)
 * - Text/textarea: preserve multiline formatting
 *
 * @example
 * ```tsx
 * <DynamicPropertiesView
 *   entityType={WorldEntityType.GeographicRegion}
 *   value={{ Climate: 'Temperate', Population: 1000000, Languages: ['English', 'Spanish'] }}
 * />
 * ```
 *
 * @param props - Component props (see DynamicPropertiesViewProps)
 * @returns Formatted property display section or null
 *
 * @public
 */
export function DynamicPropertiesView({
  entityType,
  value,
}: DynamicPropertiesViewProps) {
  // Return null if no properties
  if (!value || Object.keys(value).length === 0) {
    return null;
  }

  // T030: Fetch propertySchema from registry using getEntityTypeConfig
  const config = getEntityTypeConfig(entityType);
  const schema = config?.propertySchema;

  // T022: Section header format: "{Entity Type Label} Properties"
  const sectionTitle = `${config?.label || 'Custom'} Properties`;

  // T031: If schema exists, use schema-based rendering
  if (schema && schema.length > 0) {
    return (
      <div>
        <h2 className="text-lg font-semibold mb-3">{sectionTitle}</h2>
        <dl className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-4">
          {schema.map((fieldSchema) => {
            const fieldValue = value[fieldSchema.key];
            
            // Skip fields that are not present in the value object
            if (fieldValue === undefined) {
              return null;
            }

            return (
              <div key={fieldSchema.key} className="flex flex-col">
                <dt className="text-sm font-medium text-muted-foreground mb-1">
                  {fieldSchema.label}
                </dt>
                <dd className="text-sm">
                  {renderFormattedValue(fieldSchema.key, fieldValue, fieldSchema.type)}
                </dd>
              </div>
            );
          })}
        </dl>
      </div>
    );
  }

  // T032: Fallback to generic Object.entries() when schema missing
  return (
    <div>
      <h2 className="text-lg font-semibold mb-3">{sectionTitle}</h2>
      <dl className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-3">
        {Object.entries(value).map(([key, fieldValue]) => (
          <div key={key} className="flex flex-col">
            <dt className="text-sm font-medium text-muted-foreground capitalize">
              {key.replace(/([A-Z])/g, ' $1').trim()}
            </dt>
            <dd className="text-sm mt-1">
              {typeof fieldValue === 'object' && fieldValue !== null
                ? JSON.stringify(fieldValue, null, 2)
                : String(fieldValue)}
            </dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
