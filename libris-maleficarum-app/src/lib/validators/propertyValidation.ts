/**
 * Schema-based property validation for dynamic custom properties
 *
 * Provides validation for all property field types based on PropertyFieldSchema.
 * Includes type coercion for numeric fields to handle string-to-number conversion.
 *
 * @module lib/validators/propertyValidation
 */

import type {
  PropertyFieldSchema,
  PropertyFieldValidation,
} from '@/services/config/entityTypeRegistry';
import {
  validateInteger,
  validateDecimal,
  type NumericValidationResult,
} from './numericValidation';

/**
 * Result object returned by property field validation
 *
 * @public
 */
export interface PropertyValidationResult {
  /**
   * Whether validation passed
   * - `true`: Input is valid (or empty for optional fields)
   * - `false`: Input has validation errors
   */
  valid: boolean;

  /**
   * Human-readable error message if validation failed
   * - `undefined`: Validation passed
   * - `string`: Description of what's wrong
   */
  error?: string;

  /**
   * Coerced/normalized value if validation passed
   * - For numeric fields: Converted from string to number
   * - For other types: Returns input value as-is
   * - `undefined`: Input was empty (valid but no value)
   */
  coercedValue?: unknown;
}

/**
 * Validate required field constraint
 *
 * @param value - Field value to check
 * @param validation - Validation rules from schema
 * @returns Error message if required field is empty, undefined otherwise
 */
function validateRequired(
  value: unknown,
  validation?: PropertyFieldValidation,
): string | undefined {
  if (!validation?.required) {
    return undefined;
  }

  // Check for empty values
  if (value === undefined || value === null || value === '') {
    return 'This field is required';
  }

  // For arrays (tagArray type)
  if (Array.isArray(value) && value.length === 0) {
    return 'This field is required';
  }

  return undefined;
}

/**
 * Validate pattern constraint for text fields
 *
 * @param value - String value to validate
 * @param validation - Validation rules from schema
 * @returns Error message if pattern doesn't match, undefined otherwise
 */
function validatePattern(
  value: string,
  validation?: PropertyFieldValidation,
): string | undefined {
  if (!validation?.pattern || !value) {
    return undefined;
  }

  try {
    const regex = new RegExp(validation.pattern);
    if (!regex.test(value)) {
      return 'Invalid format';
    }
  } catch (error) {
    // Invalid regex pattern in schema - fail validation
    console.error('Invalid regex pattern in schema:', validation.pattern, error);
    return 'Invalid format';
  }

  return undefined;
}

/**
 * Validate min/max constraint for numeric fields
 *
 * @param value - Numeric value to validate
 * @param validation - Validation rules from schema
 * @returns Error message if value is out of range, undefined otherwise
 */
function validateNumericRange(
  value: number,
  validation?: PropertyFieldValidation,
): string | undefined {
  if (validation?.min !== undefined && value < validation.min) {
    return `Must be at least ${validation.min}`;
  }

  if (validation?.max !== undefined && value > validation.max) {
    return `Must be at most ${validation.max}`;
  }

  return undefined;
}

/**
 * Validate a single property field based on its schema definition
 *
 * Handles type-specific validation and type coercion for numeric fields.
 * Applies schema validation rules (required, min/max, pattern).
 *
 * **Field Type Support:**
 * - `text`: String validation with optional pattern matching
 * - `textarea`: String validation with optional pattern matching
 * - `integer`: Validates as whole number, coerces string → number
 * - `decimal`: Validates as decimal number, coerces string → number
 * - `tagArray`: Validates as array of strings
 *
 * **Type Coercion:**
 * - Integer/Decimal fields: Attempts to parse string input (e.g., "123" → 123)
 * - Shows validation error if coercion fails (e.g., "abc" → error)
 *
 * @param schema - Property field schema definition
 * @param value - Current field value (can be any type)
 * @returns Validation result with error message or coerced value
 *
 * @example
 * ```typescript
 * const schema: PropertyFieldSchema = {
 *   key: 'Population',
 *   label: 'Population',
 *   type: 'integer',
 *   validation: { required: true, min: 0 }
 * };
 *
 * validateField(schema, "1000") // { valid: true, coercedValue: 1000 }
 * validateField(schema, "abc") // { valid: false, error: "Must be a valid number" }
 * validateField(schema, "") // { valid: false, error: "This field is required" }
 * ```
 *
 * @public
 */
export function validateField(
  schema: PropertyFieldSchema,
  value: unknown,
): PropertyValidationResult {
  // Check required constraint first
  const requiredError = validateRequired(value, schema.validation);
  if (requiredError) {
    return { valid: false, error: requiredError };
  }

  // Empty non-required fields are valid
  if (
    value === undefined ||
    value === null ||
    value === '' ||
    (Array.isArray(value) && value.length === 0)
  ) {
    return { valid: true };
  }

  // Type-specific validation and coercion
  switch (schema.type) {
    case 'integer': {
      // T009: Type coercion for integer fields
      const stringValue = typeof value === 'string' ? value : String(value);
      const result: NumericValidationResult = validateInteger(stringValue);

      if (!result.valid) {
        return { valid: false, error: result.error };
      }

      // Check min/max constraints if value exists
      if (result.value !== undefined) {
        const rangeError = validateNumericRange(result.value, schema.validation);
        if (rangeError) {
          return { valid: false, error: rangeError };
        }
      }

      return { valid: true, coercedValue: result.value };
    }

    case 'decimal': {
      // T010: Type coercion for decimal fields
      const stringValue = typeof value === 'string' ? value : String(value);
      const result: NumericValidationResult = validateDecimal(stringValue);

      if (!result.valid) {
        return { valid: false, error: result.error };
      }

      // Check min/max constraints if value exists
      if (result.value !== undefined) {
        const rangeError = validateNumericRange(result.value, schema.validation);
        if (rangeError) {
          return { valid: false, error: rangeError };
        }
      }

      return { valid: true, coercedValue: result.value };
    }

    case 'text':
    case 'textarea': {
      const stringValue = typeof value === 'string' ? value : String(value);

      // Check pattern constraint
      const patternError = validatePattern(stringValue, schema.validation);
      if (patternError) {
        return { valid: false, error: patternError };
      }

      return { valid: true, coercedValue: stringValue };
    }

    case 'tagArray': {
      // Validate array type
      if (!Array.isArray(value)) {
        return { valid: false, error: 'Must be an array' };
      }

      // Validate all items are strings
      if (!value.every((item) => typeof item === 'string')) {
        return { valid: false, error: 'All tags must be strings' };
      }

      return { valid: true, coercedValue: value };
    }

    default: {
      // Unknown field type - fail validation
      const unknownType = (schema as PropertyFieldSchema).type;
      console.error('Unknown field type in schema:', unknownType);
      return { valid: false, error: 'Unknown field type' };
    }
  }
}
