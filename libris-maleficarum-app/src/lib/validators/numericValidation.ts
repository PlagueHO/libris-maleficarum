/**
 * Numeric validation utilities for entity custom properties
 *
 * Provides parsing, formatting, and validation for numeric inputs
 * (Population, Area) with support for large numbers up to Number.MAX_SAFE_INTEGER.
 *
 * @module lib/validators/numericValidation
 */

const MAX_SAFE = Number.MAX_SAFE_INTEGER; // 9,007,199,254,740,991

export interface NumericValidationResult {
  /** Whether validation passed */
  valid: boolean;

  /** Error message if validation failed */
  error?: string;

  /** Parsed numeric value if valid */
  value?: number;
}

/**
 * Parse numeric input with optional comma separators
 *
 * @param input - User input (e.g., "1,000,000" or "1000000")
 * @returns Parsed number or null if invalid
 *
 * @example
 * parseNumericInput("1,000,000") // 1000000
 * parseNumericInput("1234.56") // 1234.56
 * parseNumericInput("abc") // null
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
 * @param value - Numeric value
 * @param decimals - Decimal places (default: 0 for integers)
 * @returns Formatted string (e.g., "1,000,000")
 *
 * @example
 * formatNumericDisplay(1000000) // "1,000,000"
 * formatNumericDisplay(1234.5678, 2) // "1,234.57"
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
 * Rules:
 * - Must be a valid number
 * - Must be a whole number (no decimals)
 * - Must be non-negative
 * - Must be ≤ Number.MAX_SAFE_INTEGER
 *
 * @param input - User input string
 * @returns Validation result with error message or parsed value
 *
 * @example
 * validateInteger("1000") // { valid: true, value: 1000 }
 * validateInteger("100.5") // { valid: false, error: "Must be a whole number" }
 * validateInteger("-100") // { valid: false, error: "Must be non-negative" }
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
 * Validate decimal field (e.g., Area)
 *
 * Rules:
 * - Must be a valid number
 * - Decimals are allowed
 * - Must be non-negative
 * - Must be ≤ Number.MAX_SAFE_INTEGER
 *
 * @param input - User input string
 * @returns Validation result with error message or parsed value
 *
 * @example
 * validateDecimal("1234.56") // { valid: true, value: 1234.56 }
 * validateDecimal("1000") // { valid: true, value: 1000 }
 * validateDecimal("-100.5") // { valid: false, error: "Must be non-negative" }
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
