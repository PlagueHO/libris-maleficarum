/// <reference types="vitest/globals" />

import { describe, it, expect, beforeEach } from 'vitest';
import { setAccessCode, getAccessCode } from './apiClient';

describe('apiClient access code', () => {
  beforeEach(() => {
    setAccessCode(null);
  });

  it('should return null by default', () => {
    expect(getAccessCode()).toBeNull();
  });

  it('should set and get access code', () => {
    setAccessCode('my-code');
    expect(getAccessCode()).toBe('my-code');
  });

  it('should clear access code when set to null', () => {
    setAccessCode('my-code');
    setAccessCode(null);
    expect(getAccessCode()).toBeNull();
  });
});
