/**
 * Validation utilities barrel export
 *
 * Provides a centralized export point for all validation utilities.
 *
 * @module lib/validators
 */

// Numeric validation (Population, Area fields)
export {
  parseNumericInput,
  formatNumericDisplay,
  validateInteger,
  validateDecimal,
  type NumericValidationResult,
} from './numericValidation';

// Schema-based property validation (dynamic field rendering)
export {
  validateField,
  type PropertyValidationResult,
} from './propertyValidation';
