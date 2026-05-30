import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { MainPanelTransientAlert } from './MainPanelTransientAlert';

expect.extend(toHaveNoViolations);

describe('MainPanelTransientAlert', () => {
  it('renders error title and message as an alert', () => {
    render(
      <MainPanelTransientAlert
        title="Failed to save entry"
        message="The Name field is required."
        variant="error"
      />
    );

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('Failed to save entry')).toBeInTheDocument();
    expect(screen.getByText('The Name field is required.')).toBeInTheDocument();
  });

  it('renders warning and info variants', () => {
    const { rerender } = render(
      <MainPanelTransientAlert
        title="Connection is unstable"
        message="Edits may take longer to sync."
        variant="warning"
      />
    );

    expect(screen.getByText('Connection is unstable')).toBeInTheDocument();

    rerender(
      <MainPanelTransientAlert
        title="Autosave paused"
        message="Retrying shortly."
        variant="info"
      />
    );

    expect(screen.getByText('Autosave paused')).toBeInTheDocument();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <MainPanelTransientAlert
        title="Failed to save entry"
        message="Please review the highlighted fields and try again."
      />
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
