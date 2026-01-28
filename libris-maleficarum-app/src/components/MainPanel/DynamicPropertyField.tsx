/**
 * DynamicPropertyField Component
 *
 * Schema-driven dynamic field renderer for custom entity properties.
 * Renders appropriate input controls based on PropertyFieldSchema field type.
 *
 * Supported field types:
 * - text: Single-line text input
 * - textarea: Multi-line text input with character counter
 * - integer: Numeric input with whole number validation
 * - decimal: Numeric input with decimal validation
 * - tagArray: Tag/chip input for string arrays
 * - date: Date picker with calendar popover
 * - datetime: Date and time picker combined
 * - time: Time picker (HH:mm format)
 *
 * @module components/MainPanel/DynamicPropertyField
 */

import * as React from 'react';
import { format } from 'date-fns';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { TagInput } from '@/components/shared/TagInput';
import { DatePicker } from '@/components/ui/date-picker';
import { DateTimePicker } from '@/components/ui/datetime-picker';
import { TimePicker } from '@/components/ui/time-picker';
import type { PropertyFieldSchema } from '@/services/config/entityTypeRegistry';
import { validateField } from '@/lib/validators/propertyValidation';
import {
  formatNumericDisplay,
} from '@/lib/validators/numericValidation';

/**
 * Props for the DynamicPropertyField component
 *
 * @public
 */
export interface DynamicPropertyFieldProps {
  /**
   * Property field schema definition
   * @required
   */
  schema: PropertyFieldSchema;

  /**
   * Current field value (type varies by schema.type)
   * - text/textarea: string
   * - integer/decimal: number
   * - tagArray: string[]
   */
  value: unknown;

  /**
   * Callback fired when the field value changes
   * @required
   * @param value - New field value (type matches schema.type)
   */
  onChange: (value: unknown) => void;

  /**
   * Callback fired when validation state changes
   * @optional
   * @param hasError - True if field has validation error
   */
  onValidationChange?: (hasError: boolean) => void;

  /**
   * Whether the field is disabled
   * @optional
   * @default false
   */
  disabled?: boolean;

  /**
   * Whether the field is in read-only mode
   * @optional
   * @default false
   */
  readOnly?: boolean;
}

/**
 * DynamicPropertyField - Schema-driven dynamic field renderer
 *
 * Automatically renders the appropriate input control based on the field schema.
 * Handles validation, type coercion, and error display.
 *
 * @example
 * ```tsx
 * const schema: PropertyFieldSchema = {
 *   key: 'Population',
 *   label: 'Population',
 *   type: 'integer',
 *   placeholder: 'e.g., 1,000,000',
 *   description: 'Whole number only',
 * };
 *
 * <DynamicPropertyField
 *   schema={schema}
 *   value={1000000}
 *   onChange={(value) => setPopulation(value as number)}
 * />
 * ```
 *
 * @param props - Component props (see DynamicPropertyFieldProps)
 * @returns Appropriate input control based on schema type
 *
 * @public
 */
export function DynamicPropertyField({
  schema,
  value,
  onChange,
  onValidationChange,
  disabled = false,
  readOnly = false,
}: DynamicPropertyFieldProps) {
  // Local state for text input values (to support formatting on blur for numerics)
  const [localValue, setLocalValue] = React.useState<string>('');
  const [error, setError] = React.useState<string | undefined>();

  // Store callback in ref to avoid dependency cycles
  const onValidationChangeRef = React.useRef(onValidationChange);
  React.useEffect(() => {
    onValidationChangeRef.current = onValidationChange;
  });

  // Notify parent when validation state changes
  React.useEffect(() => {
    if (onValidationChangeRef.current) {
      onValidationChangeRef.current(!!error);
    }
  }, [error]);

  // Initialize local value from prop
  React.useEffect(() => {
    if (schema.type === 'integer' || schema.type === 'decimal') {
      // Format numeric values for display
      if (typeof value === 'number') {
        const decimals = schema.type === 'decimal' ? 2 : 0;
        setLocalValue(formatNumericDisplay(value, decimals));
      } else {
        setLocalValue('');
      }
    } else if (schema.type === 'text' || schema.type === 'textarea') {
      setLocalValue((value as string) || '');
    }
  }, [value, schema.type]);

  // Parse date value for 'date' and 'datetime' field types
  const dateValue = React.useMemo(() => {
    if (schema.type !== 'date' && schema.type !== 'datetime') return undefined;
    if (!value) return undefined;
    if (value instanceof Date) return value;
    if (typeof value === 'string') {
      const parsed = new Date(value);
      return isNaN(parsed.getTime()) ? undefined : parsed;
    }
    return undefined;
  }, [value, schema.type]);

  // T012: Text field type rendering
  if (schema.type === 'text') {
    const handleChange = (newValue: string) => {
      setLocalValue(newValue);
      onChange(newValue || undefined);
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm">{localValue || '-'}</div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <Input
          id={`field-${schema.key}`}
          type="text"
          value={localValue}
          onChange={(e) => handleChange(e.target.value)}
          placeholder={schema.placeholder}
          disabled={disabled}
          maxLength={schema.maxLength}
          aria-invalid={!!error}
          aria-describedby={error ? `${schema.key}-error` : undefined}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
        {schema.maxLength && !error && (
          <div className="text-xs text-muted-foreground mt-1">
            {localValue.length}/{schema.maxLength} characters
          </div>
        )}
      </div>
    );
  }

  // T013: Textarea field type rendering
  if (schema.type === 'textarea') {
    const handleChange = (newValue: string) => {
      setLocalValue(newValue);
      onChange(newValue || undefined);
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm whitespace-pre-wrap">{localValue || '-'}</div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <Textarea
          id={`field-${schema.key}`}
          value={localValue}
          onChange={(e) => handleChange(e.target.value)}
          placeholder={schema.placeholder}
          disabled={disabled}
          maxLength={schema.maxLength}
          className="min-h-24"
          aria-invalid={!!error}
          aria-describedby={error ? `${schema.key}-error` : undefined}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
        {schema.maxLength && !error && (
          <div className="text-xs text-muted-foreground mt-1">
            {localValue.length}/{schema.maxLength} characters
          </div>
        )}
      </div>
    );
  }

  // T014: Integer field type rendering
  if (schema.type === 'integer') {
    const handleChange = (input: string) => {
      setLocalValue(input);

      // Validate and coerce
      const validation = validateField(schema, input);
      if (!validation.valid) {
        setError(validation.error);
        onChange(undefined); // Clear parent value on error
        return;
      }

      // Update parent with coerced numeric value
      setError(undefined);
      onChange(validation.coercedValue);
    };

    const handleBlur = () => {
      // Format on blur if valid number
      if (typeof value === 'number') {
        setLocalValue(formatNumericDisplay(value));
      }
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm">
            {typeof value === 'number' ? formatNumericDisplay(value) : '-'}
          </div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <Input
          id={`field-${schema.key}`}
          type="text"
          inputMode="numeric"
          value={localValue}
          onChange={(e) => handleChange(e.target.value)}
          onBlur={handleBlur}
          placeholder={schema.placeholder}
          disabled={disabled}
          aria-invalid={!!error}
          aria-describedby={error ? `${schema.key}-error` : undefined}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
      </div>
    );
  }

  // T015: Decimal field type rendering
  if (schema.type === 'decimal') {
    const handleChange = (input: string) => {
      setLocalValue(input);

      // Validate and coerce
      const validation = validateField(schema, input);
      if (!validation.valid) {
        setError(validation.error);
        onChange(undefined); // Clear parent value on error
        return;
      }

      // Update parent with coerced numeric value
      setError(undefined);
      onChange(validation.coercedValue);
    };

    const handleBlur = () => {
      // Format on blur if valid number
      if (typeof value === 'number') {
        setLocalValue(formatNumericDisplay(value, 2));
      }
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm">
            {typeof value === 'number' ? formatNumericDisplay(value, 2) : '-'}
          </div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <Input
          id={`field-${schema.key}`}
          type="text"
          inputMode="decimal"
          value={localValue}
          onChange={(e) => handleChange(e.target.value)}
          onBlur={handleBlur}
          placeholder={schema.placeholder}
          disabled={disabled}
          aria-invalid={!!error}
          aria-describedby={error ? `${schema.key}-error` : undefined}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
      </div>
    );
  }

  // T016: TagArray field type rendering
  if (schema.type === 'tagArray') {
    const handleChange = (newTags: string[]) => {
      onChange(newTags.length > 0 ? newTags : undefined);
    };

    if (readOnly) {
      const tags = Array.isArray(value) ? value : [];
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          {tags.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {tags.map((tag) => (
                <span
                  key={tag}
                  className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-secondary text-secondary-foreground"
                >
                  {tag}
                </span>
              ))}
            </div>
          ) : (
            <div className="text-sm">-</div>
          )}
        </div>
      );
    }

    return (
      <TagInput
        label={schema.label}
        value={Array.isArray(value) ? value : []}
        onChange={handleChange}
        placeholder={schema.placeholder}
        description={schema.description}
        disabled={disabled}
        required={schema.validation?.required}
        maxLength={schema.maxLength || 50}
      />
    );
  }

  // T017: Date field type rendering
  if (schema.type === 'date') {
    const handleChange = (newDate: Date | undefined) => {
      // Store as ISO string for JSON serialization
      onChange(newDate ? newDate.toISOString() : undefined);
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm">
            {dateValue ? format(dateValue, 'PPP') : '-'}
          </div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <DatePicker
          id={`field-${schema.key}`}
          value={dateValue}
          onChange={handleChange}
          placeholder={schema.placeholder || 'Pick a date'}
          disabled={disabled}
          aria-label={schema.label}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
      </div>
    );
  }

  // T018: DateTime field type rendering
  if (schema.type === 'datetime') {
    const handleChange = (newDateTime: Date | undefined) => {
      // Store as ISO string for JSON serialization
      onChange(newDateTime ? newDateTime.toISOString() : undefined);
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm">
            {dateValue ? format(dateValue, 'PPP p') : '-'}
          </div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <DateTimePicker
          id={`field-${schema.key}`}
          value={dateValue}
          onChange={handleChange}
          placeholder={schema.placeholder || 'Pick date and time'}
          disabled={disabled}
          aria-label={schema.label}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
      </div>
    );
  }

  // T019: Time field type rendering
  if (schema.type === 'time') {
    // Time is stored as "HH:mm" string
    const timeValue = typeof value === 'string' ? value : '';

    const handleChange = (newTime: string | undefined) => {
      onChange(newTime || undefined);
    };

    if (readOnly) {
      return (
        <div>
          <div className="text-sm font-medium text-muted-foreground mb-1">
            {schema.label}
          </div>
          <div className="text-sm">{timeValue || '-'}</div>
        </div>
      );
    }

    return (
      <div>
        <label
          htmlFor={`field-${schema.key}`}
          className="block text-sm font-medium mb-3"
        >
          {schema.label}
          {schema.validation?.required && (
            <span className="text-destructive ml-1">*</span>
          )}
        </label>
        <TimePicker
          id={`field-${schema.key}`}
          value={timeValue}
          onChange={handleChange}
          placeholder={schema.placeholder || 'Pick a time'}
          disabled={disabled}
          aria-label={schema.label}
        />
        {error && (
          <span
            id={`${schema.key}-error`}
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {error}
          </span>
        )}
        {schema.description && !error && (
          <div className="text-xs text-muted-foreground mt-2">
            {schema.description}
          </div>
        )}
      </div>
    );
  }

  // Unknown field type - should never happen with proper TypeScript
  return (
    <div className="text-sm text-destructive">
      Unknown field type: {(schema as PropertyFieldSchema).type}
    </div>
  );
}
