/**
 * Numeric validation utilities for entity custom properties
 *
 * Provides parsing, formatting, and validation for numeric inputs
 * (Population, Area) with support for large numbers up to Number.MAX_SAFE_INTEGER.
 *
 * @module lib/validators/numericValidation
 */

const MAX_SAFE = Number.MAX_SAFE_INTEGER; // 9,007,199,254,740,991

/**
 * Result object returned by numeric validation functions
 *
 * @public
 */
export interface NumericValidationResult {
  /**
   * Whether validation passed
   * - `true`: Input is valid (or empty for optional fields)
   * - `false`: Input has validation errors
   */
  valid: boolean;

  /**
   * Human-readable error message if validation failed
   * - `undefined`: Validation passed
   * - `string`: Description of what's wrong (e.g., "Must be a whole number")
   */
  error?: string;

  /**
   * Parsed numeric value if validation passed
   * - `undefined`: Input was empty (valid but no value)
   * - `number`: Successfully parsed and validated number
   */
  value?: number;
}

/**
 * Parse numeric input with optional comma separators
 *
 * Handles user-friendly number formats with thousands separators.
 * Supports positive/negative integers and decimals.
 *
 * @param input - User input (e.g., "1,000,000" or "1000000")
 * @returns Parsed number or null if invalid
 *
 * @example
 * parseNumericInput("1,000,000") // 1000000
 * parseNumericInput("1234.56") // 1234.56
 * parseNumericInput("abc") // null
 * parseNumericInput("") // null (empty input)
 * parseNumericInput("-500") // -500 (negative numbers supported)
 *
 * @remarks
 * - Commas are stripped before parsing
 * - Empty/whitespace-only input returns `null`
 * - Invalid formats (letters, multiple decimals) return `null`
 * - Scientific notation is NOT supported
 *
 * @public
 */
export function parseNumericInput(input: string): number | null {
  if (!input || input.trim() === '') {
    return null;
  }

  // Remove commas and trim whitespace
  const cleaned = input.replace(/,/g, '').trim();

  // Check if it's a valid number format (optional negative, digits, optional decimal)
  if (!/^-?\d+(\.\d+)?$/.test(cleaned)) {
    return null;
  }

  const num = parseFloat(cleaned);
  return isNaN(num) ? null : num;
}

/**
 * Format number with thousand separators for display
 *
 * Uses `Intl.NumberFormat` with US English locale (commas as thousands separator).
 * Rounds decimal values to specified precision.
 *
 * @param value - Numeric value to format
 * @param decimals - Decimal places to display (default: 0 for integers)
 * @returns Formatted string with thousand separators
 *
 * @example
 * formatNumericDisplay(1000000) // "1,000,000"
 * formatNumericDisplay(1234.5678, 2) // "1,234.57"
 * formatNumericDisplay(999.999, 0) // "1,000" (rounds up)
 * formatNumericDisplay(0) // "0"
 *
 * @remarks
 * - Locale is hardcoded to 'en-US' (commas for thousands, periods for decimals)
 * - Rounding uses default banker's rounding (round half to even)
 * - For currency formatting, use `Intl.NumberFormat` with currency options instead
 *
 * @public
 */
export function formatNumericDisplay(
  value: number,
  decimals: number = 0
): string {
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);
}

/**
 * Validate integer field (e.g., Population)
 *
 * Enforces whole number constraints for fields that cannot have fractional values.
 * Accepts comma-separated input (e.g., "1,000,000").
 *
 * **Validation Rules:**
 * - Must be a valid number (parseable)
 * - Must be a whole number (no decimals)
 * - Must be non-negative (≥ 0)
 * - Must be ≤ Number.MAX_SAFE_INTEGER (9,007,199,254,740,991)
 * - Empty input is valid (optional field)
 *
 * @param input - User input string (can include commas)
 * @returns Validation result with error message or parsed value
 *
 * @example
 * validateInteger("1000") // { valid: true, value: 1000 }
 * validateInteger("1,000,000") // { valid: true, value: 1000000 }
 * validateInteger("100.5") // { valid: false, error: "Must be a whole number" }
 * validateInteger("-100") // { valid: false, error: "Must be non-negative" }
 * validateInteger("") // { valid: true } (empty is valid for optional fields)
 * validateInteger("abc") // { valid: false, error: "Must be a valid number" }
 *
 * @remarks
 * - Use this for Population, Quantity, Count fields
 * - For decimal values (Area, Distance), use `validateDecimal` instead
 * - MAX_SAFE_INTEGER limit prevents precision loss in JavaScript numbers
 *
 * @public
 */
export function validateInteger(input: string): NumericValidationResult {
  if (!input || input.trim() === '') {
    // Empty input is valid (optional field)
    return { valid: true };
  }

  const value = parseNumericInput(input);

  if (value === null) {
    return { valid: false, error: 'Must be a valid number' };
  }

  if (!Number.isInteger(value)) {
    return { valid: false, error: 'Must be a whole number' };
  }

  if (value < 0) {
    return { valid: false, error: 'Must be non-negative' };
  }

  if (value > MAX_SAFE) {
    return {
      valid: false,
      error: `Must be less than ${formatNumericDisplay(MAX_SAFE)}`,
    };
  }

  return { valid: true, value };
}

/**
 * Validate decimal field (e.g., Area, Distance, Measurement)
 *
 * Accepts fractional values for fields requiring precision.
 * Accepts comma-separated input (e.g., "150,000.50").
 *
 * **Validation Rules:**
 * - Must be a valid number (parseable)
 * - Decimals are allowed (e.g., 123.45)
 * - Must be non-negative (≥ 0)
 * - Must be ≤ Number.MAX_SAFE_INTEGER (9,007,199,254,740,991)
 * - Empty input is valid (optional field)
 *
 * @param input - User input string (can include commas and decimals)
 * @returns Validation result with error message or parsed value
 *
 * @example
 * validateDecimal("1234.56") // { valid: true, value: 1234.56 }
 * validateDecimal("150,000.50") // { valid: true, value: 150000.5 }
 * validateDecimal("1000") // { valid: true, value: 1000 } (whole numbers OK)
 * validateDecimal("-100.5") // { valid: false, error: "Must be non-negative" }
 * validateDecimal("") // { valid: true } (empty is valid for optional fields)
 * validateDecimal("12.34.56") // { valid: false, error: "Must be a valid number" }
 *
 * @remarks
 * - Use this for Area, Distance, Coordinates, Measurements
 * - For whole numbers only (Population, Count), use `validateInteger` instead
 * - No limit on decimal precision, but IEEE 754 applies (~15-17 significant digits)
 * - Values beyond MAX_SAFE_INTEGER lose precision and are rejected
 *
 * @public
 */
export function validateDecimal(input: string): NumericValidationResult {
  if (!input || input.trim() === '') {
    // Empty input is valid (optional field)
    return { valid: true };
  }

  const value = parseNumericInput(input);

  if (value === null) {
    return { valid: false, error: 'Must be a valid number' };
  }

  if (value < 0) {
    return { valid: false, error: 'Must be non-negative' };
  }

  if (value > MAX_SAFE) {
    return {
      valid: false,
      error: `Must be less than ${formatNumericDisplay(MAX_SAFE)}`,
    };
  }

  return { valid: true, value };
}
