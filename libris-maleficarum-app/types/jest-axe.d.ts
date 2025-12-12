declare module "jest-axe" {
  import type { AxeResults, RunOptions } from "axe-core";

  export function axe(
    html: Element | Document,
    options?: RunOptions
  ): Promise<AxeResults>;

  export function toHaveNoViolations(): {
    toHaveNoViolations(results: AxeResults): {
      message(): string;
      pass: boolean;
    };
  };

  export function configureAxe(options?: RunOptions): typeof axe;
}
