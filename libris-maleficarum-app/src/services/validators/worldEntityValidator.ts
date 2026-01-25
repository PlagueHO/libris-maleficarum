/**
 * WorldEntity Validation Module
 *
 * Schema-based validation for world entities using entity type registry.
 * Provides client-side validation rules that mirror server-side constraints.
 *
 * Validation Rules:
 * - Name: Required, 1-100 characters (after trim)
 * - Entity Type: Required in create mode, read-only in edit mode
 * - Description: Optional, 0-500 characters
 * - Custom Properties: Must be JSON-serializable, type-specific schemas (future)
 *
 * @module services/validators/worldEntityValidator
 */

import type { WorldEntityType } from '../types/worldEntity.types';

/**
 * Validation error object for a single field
 */
export interface FieldValidationError {
  field: string;
  message: string;
}

/**
 * Validation result containing all errors
 */
export interface ValidationResult {
  isValid: boolean;
  errors: Record<string, string>; // field name → error message
}

/**
 * Data to validate (subset of WorldEntity)
 */
export interface WorldEntityFormData {
  name: string;
  description?: string | null;
  entityType?: WorldEntityType | '';
  customProperties?: Record<string, unknown> | null;
}

/**
 * Validation constraints
 */
const CONSTRAINTS = {
  NAME: {
    MIN_LENGTH: 1,
    MAX_LENGTH: 100,
  },
  DESCRIPTION: {
    MAX_LENGTH: 500,
  },
} as const;

/**
 * Error messages
 */
const ERROR_MESSAGES = {
  NAME_REQUIRED: 'Name is required',
  NAME_TOO_LONG: `Name must be ${CONSTRAINTS.NAME.MAX_LENGTH} characters or less`,
  TYPE_REQUIRED: 'Type is required',
  DESCRIPTION_TOO_LONG: `Description must be ${CONSTRAINTS.DESCRIPTION.MAX_LENGTH} characters or less`,
  PROPERTIES_NOT_SERIALIZABLE: 'Custom properties must be valid JSON',
} as const;

/**
 * Validate entity name field
 *
 * @param name - The entity name to validate
 * @returns Error message if invalid, null if valid
 */
export function validateName(name: string): string | null {
  const trimmedName = name.trim();

  if (trimmedName.length === 0) {
    return ERROR_MESSAGES.NAME_REQUIRED;
  }

  if (trimmedName.length > CONSTRAINTS.NAME.MAX_LENGTH) {
    return ERROR_MESSAGES.NAME_TOO_LONG;
  }

  return null;
}

/**
 * Validate entity type field
 *
 * @param entityType - The entity type to validate
 * @param isEditMode - Whether form is in edit mode (type is read-only)
 * @returns Error message if invalid, null if valid
 */
export function validateEntityType(
  entityType: WorldEntityType | '' | undefined,
  isEditMode: boolean = false
): string | null {
  // In edit mode, type is read-only so skip validation
  if (isEditMode) {
    return null;
  }

  // In create mode, type is required
  if (!entityType) {
    return ERROR_MESSAGES.TYPE_REQUIRED;
  }

  return null;
}

/**
 * Validate description field
 *
 * @param description - The description to validate
 * @returns Error message if invalid, null if valid
 */
export function validateDescription(description: string | null | undefined): string | null {
  // Description is optional - empty/null is valid
  if (!description) {
    return null;
  }

  if (description.length > CONSTRAINTS.DESCRIPTION.MAX_LENGTH) {
    return ERROR_MESSAGES.DESCRIPTION_TOO_LONG;
  }

  return null;
}

/**
 * Validate custom properties are JSON-serializable
 *
 * @param properties - The custom properties object
 * @returns Error message if invalid, null if valid
 */
export function validateCustomProperties(
  properties: Record<string, unknown> | null | undefined
): string | null {
  // Properties are optional
  if (!properties || Object.keys(properties).length === 0) {
    return null;
  }

  // Attempt to serialize to verify it's valid JSON
  try {
    JSON.stringify(properties);
    return null;
  } catch {
    return ERROR_MESSAGES.PROPERTIES_NOT_SERIALIZABLE;
  }
}

/**
 * Validate all fields of a WorldEntity form
 *
 * This is the primary validation function used by WorldEntityForm component.
 *
 * @param data - Form data to validate
 * @param isEditMode - Whether form is in edit mode
 * @returns Validation result with isValid flag and error messages by field
 *
 * @example
 * ```typescript
 * const result = validateWorldEntityForm({
 *   name: '',
 *   entityType: '',
 *   description: 'Valid description',
 * }, false);
 *
 * if (!result.isValid) {
 *   console.log(result.errors); // { name: 'Name is required', type: 'Type is required' }
 * }
 * ```
 */
export function validateWorldEntityForm(
  data: WorldEntityFormData,
  isEditMode: boolean = false
): ValidationResult {
  const errors: Record<string, string> = {};

  // Validate name
  const nameError = validateName(data.name);
  if (nameError) {
    errors.name = nameError;
  }

  // Validate entity type (only in create mode)
  const typeError = validateEntityType(data.entityType, isEditMode);
  if (typeError) {
    errors.type = typeError;
  }

  // Validate description
  const descriptionError = validateDescription(data.description);
  if (descriptionError) {
    errors.description = descriptionError;
  }

  // Validate custom properties
  const propertiesError = validateCustomProperties(data.customProperties);
  if (propertiesError) {
    errors.properties = propertiesError;
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
  };
}

/**
 * Clear a specific field error
 *
 * Utility function to remove an error for a field after it's been corrected.
 *
 * @param errors - Current errors object
 * @param field - Field name to clear
 * @returns New errors object with specified field removed
 *
 * @example
 * ```typescript
 * const errors = { name: 'Name is required', type: 'Type is required' };
 * const newErrors = clearFieldError(errors, 'name');
 * console.log(newErrors); // { type: 'Type is required' }
 * ```
 */
export function clearFieldError(
  errors: Record<string, string>,
  field: string
): Record<string, string> {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { [field]: _removed, ...rest } = errors;
  return rest;
}

/**
 * Get validation constraints (for displaying limits in UI)
 */
export function getValidationConstraints() {
  return CONSTRAINTS;
}
