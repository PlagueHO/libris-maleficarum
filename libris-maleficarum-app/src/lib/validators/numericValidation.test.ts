/**
 * Tests for numeric validation utilities
 *
 * @module lib/validators/numericValidation.test
 */

import { describe, it, expect } from 'vitest';
import {
  parseNumericInput,
  formatNumericDisplay,
  validateInteger,
  validateDecimal,
} from './numericValidation';

describe('parseNumericInput', () => {
  it('parses number with commas', () => {
    expect(parseNumericInput('1,000,000')).toBe(1000000);
  });

  it('parses plain number without commas', () => {
    expect(parseNumericInput('1000000')).toBe(1000000);
  });

  it('parses decimal number', () => {
    expect(parseNumericInput('1234.56')).toBe(1234.56);
  });

  it('parses decimal with commas', () => {
    expect(parseNumericInput('1,234.56')).toBe(1234.56);
  });

  it('parses negative number', () => {
    expect(parseNumericInput('-1000')).toBe(-1000);
  });

  it('trims whitespace', () => {
    expect(parseNumericInput('  1000  ')).toBe(1000);
  });

  it('returns null for empty string', () => {
    expect(parseNumericInput('')).toBeNull();
  });

  it('returns null for whitespace only', () => {
    expect(parseNumericInput('   ')).toBeNull();
  });

  it('returns null for invalid input', () => {
    expect(parseNumericInput('abc')).toBeNull();
    expect(parseNumericInput('12abc')).toBeNull();
    expect(parseNumericInput('12.34.56')).toBeNull();
  });

  it('handles zero', () => {
    expect(parseNumericInput('0')).toBe(0);
  });

  it('handles very large numbers', () => {
    expect(parseNumericInput('9007199254740991')).toBe(9007199254740991);
  });
});

describe('formatNumericDisplay', () => {
  it('formats integer with thousand separators', () => {
    expect(formatNumericDisplay(1000000)).toBe('1,000,000');
  });

  it('formats small number', () => {
    expect(formatNumericDisplay(123)).toBe('123');
  });

  it('formats zero', () => {
    expect(formatNumericDisplay(0)).toBe('0');
  });

  it('formats decimal with specified precision', () => {
    expect(formatNumericDisplay(1234.5678, 2)).toBe('1,234.57');
  });

  it('formats with no decimals by default', () => {
    expect(formatNumericDisplay(1234.5678)).toBe('1,235');
  });

  it('formats very large numbers', () => {
    expect(formatNumericDisplay(9007199254740991)).toBe(
      '9,007,199,254,740,991'
    );
  });

  it('pads decimals to specified precision', () => {
    expect(formatNumericDisplay(1000.5, 2)).toBe('1,000.50');
  });
});

describe('validateInteger', () => {
  describe('valid cases', () => {
    it('validates positive integer', () => {
      const result = validateInteger('1000');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(1000);
    });

    it('validates zero', () => {
      const result = validateInteger('0');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(0);
    });

    it('validates integer with commas', () => {
      const result = validateInteger('1,000,000');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(1000000);
    });

    it('validates empty string as optional field', () => {
      const result = validateInteger('');
      expect(result.valid).toBe(true);
      expect(result.value).toBeUndefined();
    });

    it('validates whitespace as optional field', () => {
      const result = validateInteger('   ');
      expect(result.valid).toBe(true);
      expect(result.value).toBeUndefined();
    });

    it('validates MAX_SAFE_INTEGER', () => {
      const result = validateInteger('9007199254740991');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(9007199254740991);
    });
  });

  describe('invalid cases', () => {
    it('rejects decimal number', () => {
      const result = validateInteger('100.5');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('whole number');
    });

    it('rejects negative number', () => {
      const result = validateInteger('-100');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('non-negative');
    });

    it('rejects non-numeric input', () => {
      const result = validateInteger('abc');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('valid number');
    });

    it('rejects number greater than MAX_SAFE_INTEGER', () => {
      const result = validateInteger('9007199254740992');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('less than');
    });

    it('rejects mixed alphanumeric', () => {
      const result = validateInteger('100abc');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('valid number');
    });
  });
});

describe('validateDecimal', () => {
  describe('valid cases', () => {
    it('validates positive decimal', () => {
      const result = validateDecimal('1234.56');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(1234.56);
    });

    it('validates integer as decimal', () => {
      const result = validateDecimal('1000');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(1000);
    });

    it('validates zero', () => {
      const result = validateDecimal('0');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(0);
    });

    it('validates decimal with commas', () => {
      const result = validateDecimal('1,234,567.89');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(1234567.89);
    });

    it('validates empty string as optional field', () => {
      const result = validateDecimal('');
      expect(result.valid).toBe(true);
      expect(result.value).toBeUndefined();
    });

    it('validates whitespace as optional field', () => {
      const result = validateDecimal('   ');
      expect(result.valid).toBe(true);
      expect(result.value).toBeUndefined();
    });

    it('validates MAX_SAFE_INTEGER', () => {
      const result = validateDecimal('9007199254740991');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(9007199254740991);
    });

    it('validates decimal with many decimal places', () => {
      const result = validateDecimal('123.456789');
      expect(result.valid).toBe(true);
      expect(result.value).toBe(123.456789);
    });
  });

  describe('invalid cases', () => {
    it('rejects negative number', () => {
      const result = validateDecimal('-100.5');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('non-negative');
    });

    it('rejects non-numeric input', () => {
      const result = validateDecimal('abc');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('valid number');
    });

    it('rejects number greater than MAX_SAFE_INTEGER', () => {
      const result = validateDecimal('9007199254740992');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('less than');
    });

    it('rejects mixed alphanumeric', () => {
      const result = validateDecimal('100.5abc');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('valid number');
    });

    it('rejects multiple decimal points', () => {
      const result = validateDecimal('100.50.25');
      expect(result.valid).toBe(false);
      expect(result.error).toContain('valid number');
    });
  });
});
