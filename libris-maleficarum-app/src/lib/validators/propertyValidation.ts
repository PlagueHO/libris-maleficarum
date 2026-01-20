/**
 * Shared validation utilities for custom property components
 *
 * Provides common patterns for text field validation, length checking,
 * and error state management used across property components.
 *
 * @module lib/validators/propertyValidation
 */

/**
 * Validation result for text field validation
 *
 * @public
 */
export interface TextValidationResult {
  /**
   * Whether validation passed
   */
  valid: boolean;

  /**
   * Error message if validation failed
   */
  error?: string;

  /**
   * Trimmed and validated text value
   */
  value?: string;
}

/**
 * Validate text field with max length constraint
 *
 * Common pattern for validating text inputs in custom property forms.
 * Trims whitespace and checks against maximum character limit.
 *
 * @param input - User input string
 * @param maxLength - Maximum allowed length (default: 200)
 * @param fieldName - Name of field for error messages (default: "Field")
 * @returns Validation result with error or trimmed value
 *
 * @example
 * validateTextField("  Hello  ", 10, "Climate")
 * // { valid: true, value: "Hello" }
 *
 * validateTextField("Very long text...", 5, "Terrain")
 * // { valid: false, error: "Terrain must be 5 characters or less" }
 *
 * validateTextField("", 100)
 * // { valid: true, value: "" } (empty is valid)
 *
 * @public
 */
export function validateTextField(
  input: string,
  maxLength: number = 200,
  fieldName: string = 'Field'
): TextValidationResult {
  const trimmed = input.trim();

  if (trimmed.length > maxLength) {
    return {
      valid: false,
      error: `${fieldName} must be ${maxLength} characters or less`,
    };
  }

  return { valid: true, value: trimmed };
}

/**
 * Validate array of strings (tags/lists) with individual item length limits
 *
 * Common pattern for validating tag inputs in custom property forms.
 *
 * @param items - Array of string values
 * @param maxItemLength - Maximum length per individual item (default: 50)
 * @param fieldName - Name of field for error messages (default: "Item")
 * @returns Validation result with error or trimmed values
 *
 * @example
 * validateArrayField(["English", "Spanish"], 20, "Language")
 * // { valid: true, value: ["English", "Spanish"] }
 *
 * validateArrayField(["Very long language name..."], 10, "Language")
 * // { valid: false, error: "Language must be 10 characters or less" }
 *
 * validateArrayField([], 50)
 * // { valid: true, value: [] } (empty is valid)
 *
 * @public
 */
export function validateArrayField(
  items: string[],
  maxItemLength: number = 50,
  fieldName: string = 'Item'
): { valid: boolean; error?: string; value?: string[] } {
  const trimmedItems = items.map((item) => item.trim());

  // Check each item length
  const tooLongItem = trimmedItems.find((item) => item.length > maxItemLength);
  if (tooLongItem) {
    return {
      valid: false,
      error: `${fieldName} must be ${maxItemLength} characters or less`,
    };
  }

  return { valid: true, value: trimmedItems };
}

/**
 * Check if a value has changed and is non-empty
 *
 * Helper for determining when to update parent state.
 * Returns true if value should be sent to parent (non-empty after trimming).
 *
 * @param value - Current value
 * @returns True if value should be persisted (non-empty)
 *
 * @example
 * shouldPersistValue("  ") // false (whitespace only)
 * shouldPersistValue("") // false (empty)
 * shouldPersistValue("Hello") // true
 * shouldPersistValue(undefined) // false
 *
 * @public
 */
export function shouldPersistValue(value: string | undefined): boolean {
  return !!value && value.trim().length > 0;
}

/**
 * Check if an array has changed and has items
 *
 * Helper for determining when to update parent state with array values.
 * Returns true if array should be sent to parent (has items).
 *
 * @param items - Array to check
 * @returns True if array should be persisted (has items)
 *
 * @example
 * shouldPersistArray([]) // false
 * shouldPersistArray(["item"]) // true
 * shouldPersistArray(undefined) // false
 *
 * @public
 */
export function shouldPersistArray(items: string[] | undefined): boolean {
  return !!items && items.length > 0;
}
