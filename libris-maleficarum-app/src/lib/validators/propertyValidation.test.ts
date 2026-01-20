/**
 * Tests for propertyValidation utilities
 */

import { describe, it, expect } from 'vitest';
import {
  validateTextField,
  validateArrayField,
  shouldPersistValue,
  shouldPersistArray,
} from './propertyValidation';

describe('propertyValidation', () => {
  describe('validateTextField', () => {
    it('validates text within max length', () => {
      const result = validateTextField('Hello World', 20, 'Climate');
      expect(result.valid).toBe(true);
      expect(result.value).toBe('Hello World');
      expect(result.error).toBeUndefined();
    });

    it('trims whitespace from input', () => {
      const result = validateTextField('  Hello  ', 10, 'Terrain');
      expect(result.valid).toBe(true);
      expect(result.value).toBe('Hello');
    });

    it('rejects text exceeding max length', () => {
      const result = validateTextField('Very long text here', 5, 'Climate');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Climate must be 5 characters or less');
    });

    it('accepts empty string', () => {
      const result = validateTextField('', 100, 'Field');
      expect(result.valid).toBe(true);
      expect(result.value).toBe('');
    });

    it('uses default max length of 200', () => {
      const longText = 'a'.repeat(201);
      const result = validateTextField(longText, undefined, 'Field');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Field must be 200 characters or less');
    });

    it('uses default field name', () => {
      const result = validateTextField('Too long', 5);
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Field must be 5 characters or less');
    });
  });

  describe('validateArrayField', () => {
    it('validates array with items within max length', () => {
      const result = validateArrayField(['English', 'Spanish'], 20, 'Language');
      expect(result.valid).toBe(true);
      expect(result.value).toEqual(['English', 'Spanish']);
      expect(result.error).toBeUndefined();
    });

    it('trims whitespace from items', () => {
      const result = validateArrayField(['  English  ', ' Spanish '], 20, 'Language');
      expect(result.valid).toBe(true);
      expect(result.value).toEqual(['English', 'Spanish']);
    });

    it('rejects array with item exceeding max length', () => {
      const result = validateArrayField(['Very long language name'], 10, 'Language');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Language must be 10 characters or less');
    });

    it('accepts empty array', () => {
      const result = validateArrayField([], 50, 'Tag');
      expect(result.valid).toBe(true);
      expect(result.value).toEqual([]);
    });

    it('uses default max item length of 50', () => {
      const longItem = 'a'.repeat(51);
      const result = validateArrayField([longItem], undefined, 'Tag');
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Tag must be 50 characters or less');
    });

    it('uses default field name', () => {
      const result = validateArrayField(['Too long item'], 5);
      expect(result.valid).toBe(false);
      expect(result.error).toBe('Item must be 5 characters or less');
    });
  });

  describe('shouldPersistValue', () => {
    it('returns true for non-empty string', () => {
      expect(shouldPersistValue('Hello')).toBe(true);
    });

    it('returns false for empty string', () => {
      expect(shouldPersistValue('')).toBe(false);
    });

    it('returns false for whitespace-only string', () => {
      expect(shouldPersistValue('   ')).toBe(false);
    });

    it('returns false for undefined', () => {
      expect(shouldPersistValue(undefined)).toBe(false);
    });
  });

  describe('shouldPersistArray', () => {
    it('returns true for array with items', () => {
      expect(shouldPersistArray(['item'])).toBe(true);
    });

    it('returns true for array with multiple items', () => {
      expect(shouldPersistArray(['item1', 'item2'])).toBe(true);
    });

    it('returns false for empty array', () => {
      expect(shouldPersistArray([])).toBe(false);
    });

    it('returns false for undefined', () => {
      expect(shouldPersistArray(undefined)).toBe(false);
    });
  });
});
