/**
 * @file Skeleton component tests
 */

import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Skeleton } from '@/components/ui/skeleton';

expect.extend(toHaveNoViolations);

describe('Skeleton', () => {
  it('renders with default classes', () => {
    const { container } = render(<Skeleton />);
    const skeleton = container.firstChild as HTMLElement;

    expect(skeleton).toHaveClass('bg-gradient-to-r');
    expect(skeleton).toHaveClass('from-muted');
    expect(skeleton).toHaveClass('rounded-md');
    expect(skeleton).toHaveClass('opacity-20');
  });

  it('accepts custom className', () => {
    const { container } = render(<Skeleton className="w-full h-9" />);
    const skeleton = container.firstChild as HTMLElement;

    expect(skeleton).toHaveClass('w-full');
    expect(skeleton).toHaveClass('h-9');
    expect(skeleton).toHaveClass('bg-gradient-to-r'); // Still has base classes
  });

  it('spreads additional props', () => {
    const { container } = render(<Skeleton data-testid="loading-skeleton" />);
    const skeleton = container.firstChild as HTMLElement;

    expect(skeleton).toHaveAttribute('data-testid', 'loading-skeleton');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Skeleton />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('can be used as loading placeholder', () => {
    const { container } = render(
      <div role="status" aria-label="Loading">
        <Skeleton className="h-8 w-32" />
      </div>
    );

    expect(container.querySelector('[role="status"]')).toBeInTheDocument();
  });
});
