/**
 * DynamicPropertiesForm Component
 *
 * Schema-driven dynamic form renderer for entity custom properties in edit mode.
 * Renders property fields based on the entity type's propertySchema from the registry.
 *
 * Features:
 * - Automatic field generation from schema
 * - Section header with entity type-specific title
 * - Aggregates field values into single properties object
 * - Tracks validation errors across all fields
 * - Handles entities without propertySchema (renders nothing)
 *
 * @module components/MainPanel/DynamicPropertiesForm
 */

import * as React from 'react';
import type { WorldEntityType } from '@/services/types/worldEntity.types';
import { getEntityTypeConfig } from '@/services/config/entityTypeRegistry';
import { DynamicPropertyField } from './DynamicPropertyField';

/**
 * Props for the DynamicPropertiesForm component
 *
 * @public
 */
export interface DynamicPropertiesFormProps {
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

  /**
   * Callback fired when any property field changes
   * @required
   * @param properties - Updated properties object with all field values
   */
  onChange: (properties: Record<string, unknown> | null) => void;

  /**
   * Callback fired when validation state changes
   * @optional
   * @param hasErrors - True if any field has validation errors
   */
  onValidationChange?: (hasErrors: boolean) => void;

  /**
   * Whether all fields are disabled
   * @optional
   * @default false
   */
  disabled?: boolean;
}

/**
 * DynamicPropertiesForm - Schema-driven edit mode form renderer
 *
 * Dynamically generates a form section with property fields based on the
 * entity type's propertySchema from the registry. Updates parent component
 * with aggregated field values.
 *
 * **Behavior**:
 * - If entity type has no `propertySchema`, renders nothing (FR-008)
 * - Section header format: `"{Entity Type Label} Properties"` (e.g., "Geographic Properties")
 * - Iterates schema fields in order, rendering DynamicPropertyField for each
 * - Aggregates all field values into single object passed to onChange
 *
 * @example
 * ```tsx
 * <DynamicPropertiesForm
 *   entityType={WorldEntityType.GeographicRegion}
 *   value={{ Climate: 'Temperate', Population: 1000000 }}
 *   onChange={(props) => setCustomProperties(props)}
 *   disabled={isSubmitting}
 * />
 * ```
 *
 * @param props - Component props (see DynamicPropertiesFormProps)
 * @returns Schema-driven property form section or null
 *
 * @public
 */
export function DynamicPropertiesForm({
  entityType,
  value,
  onChange,
  onValidationChange,
  disabled = false,
}: DynamicPropertiesFormProps) {
  // T047: Track validation errors for all fields
  const [fieldErrors, setFieldErrors] = React.useState<Record<string, boolean>>({});

  // Store callback in ref to avoid dependency cycles
  const onValidationChangeRef = React.useRef(onValidationChange);
  React.useEffect(() => {
    onValidationChangeRef.current = onValidationChange;
  });

  // T020: Fetch propertySchema from registry using getEntityTypeConfig
  const config = getEntityTypeConfig(entityType);
  const schema = config?.propertySchema;

  // T047: Notify parent when validation state changes
  React.useEffect(() => {
    if (onValidationChangeRef.current) {
      const hasErrors = Object.values(fieldErrors).some((hasError) => hasError);
      onValidationChangeRef.current(hasErrors);
    }
  }, [fieldErrors]);

  // T021: If no schema, render nothing (FR-008)
  if (!schema || schema.length === 0) {
    return null;
  }

  // T022: Section header format: "{Entity Type Label} Properties"
  const sectionTitle = `${config.label} Properties`;

  /**
   * Handle individual field value changes
   * Aggregates all field values into single properties object
   *
   * @param fieldKey - Schema field key
   * @param fieldValue - New field value (type varies by field type)
   */
  const handleFieldChange = (fieldKey: string, fieldValue: unknown) => {
    const updatedProperties = {
      ...(value || {}),
      [fieldKey]: fieldValue,
    };

    // Remove undefined values (T026: empty/undefined property filtering)
    Object.keys(updatedProperties).forEach((key) => {
      if (updatedProperties[key] === undefined) {
        delete updatedProperties[key];
      }
    });

    // If no properties remain, set to null instead of empty object
    const hasProperties = Object.keys(updatedProperties).length > 0;
    onChange(hasProperties ? updatedProperties : null);
  };

  /**
   * Handle validation state changes for individual fields
   * T047: Track validation errors per field
   *
   * @param fieldKey - Schema field key
   * @param hasError - Whether the field has a validation error
   */
  const handleFieldValidationChange = (fieldKey: string, hasError: boolean) => {
    setFieldErrors((prev) => ({
      ...prev,
      [fieldKey]: hasError,
    }));
  };

  return (
    <div className="border-t pt-6 mt-6">
      {/* T022: Section header with entity type-specific title */}
      <h3 className="text-lg font-semibold border-b pb-2 mb-6">
        {sectionTitle}
      </h3>

      {/* T021: Iterate over schema fields and render DynamicPropertyField for each */}
      <div className="space-y-6">
        {schema.map((fieldSchema) => (
          <DynamicPropertyField
            key={fieldSchema.key}
            schema={fieldSchema}
            value={value?.[fieldSchema.key]}
            onChange={(newValue) => handleFieldChange(fieldSchema.key, newValue)}
            onValidationChange={(hasError) =>
              handleFieldValidationChange(fieldSchema.key, hasError)
            }
            disabled={disabled}
            readOnly={false}
          />
        ))}
      </div>
    </div>
  );
}
