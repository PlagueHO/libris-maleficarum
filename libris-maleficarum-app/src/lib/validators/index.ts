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

// Property validation (common text field patterns)
export {
  validateTextField,
  validateArrayField,
  shouldPersistValue,
  shouldPersistArray,
  type TextValidationResult,
} from './propertyValidation';
