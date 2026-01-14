/**
 * Type declarations for jest-axe
 *
 * Provides TypeScript types for the jest-axe accessibility testing library.
 */

import type { AxeResults, RunOptions, Spec } from 'axe-core';

declare module 'jest-axe' {
  /**
   * Run axe accessibility tests on the provided HTML element
   */
  export function axe(
    html: Element | Document | string,
    options?: RunOptions | Spec
  ): Promise<AxeResults>;

  /**
   * Jest/Vitest matcher to assert that axe results have no violations
   */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  export const toHaveNoViolations: any;

  /**
   * Configure axe instance
   */
  export function configureAxe(options?: RunOptions | Spec): typeof axe;
}

declare module 'vitest' {
  interface Assertion<T = unknown> {
    toHaveNoViolations(): T;
  }
  interface AsymmetricMatchersContaining {
    toHaveNoViolations(): unknown;
  }
}
