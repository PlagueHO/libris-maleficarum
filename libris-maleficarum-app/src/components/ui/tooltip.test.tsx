/**
 * @file Tooltip component tests
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import userEvent from '@testing-library/user-event';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';

expect.extend(toHaveNoViolations);

function renderTooltip(content = 'Tooltip text') {
  return render(
    <TooltipProvider delayDuration={0}>
      <Tooltip defaultOpen>
        <TooltipTrigger asChild>
          <button>Hover me</button>
        </TooltipTrigger>
        <TooltipContent>{content}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

describe('Tooltip', () => {
  it('renders tooltip content when open', () => {
    renderTooltip('Test tooltip');
    expect(screen.getByRole('tooltip')).toHaveTextContent('Test tooltip');
  });

  it('uses theme token classes instead of hardcoded colours', () => {
    renderTooltip();
    // Radix renders the tooltip in a portal; query the full document
    // for the styled content element (not the role="tooltip" wrapper)
    const styledEl = document.querySelector('[data-side]');
    expect(styledEl).not.toBeNull();

    const classes = styledEl!.className;
    expect(classes).toContain('bg-popover');
    expect(classes).toContain('text-popover-foreground');
    expect(classes).toContain('border-border');
    expect(classes).not.toContain('bg-slate');
    expect(classes).not.toContain('text-slate');
    expect(classes).not.toContain('border-slate');
  });

  it('has no accessibility violations', async () => {
    const { container } = renderTooltip();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('shows on trigger interaction', async () => {
    const user = userEvent.setup();
    render(
      <TooltipProvider delayDuration={0}>
        <Tooltip>
          <TooltipTrigger asChild>
            <button>Hover target</button>
          </TooltipTrigger>
          <TooltipContent>Visible tooltip</TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );

    const trigger = screen.getByRole('button', { name: 'Hover target' });
    await user.hover(trigger);
    expect(await screen.findByRole('tooltip')).toHaveTextContent(
      'Visible tooltip'
    );
  });
});
