/**
 * EmptyState Component Tests
 *
 * Tests for the empty state prompt displayed when a user has no worlds.
 * Follows AAA pattern: Arrange, Act, Assert.
 *
 * @see EmptyState.tsx
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EmptyState } from './EmptyState';

expect.extend(toHaveNoViolations);

describe('EmptyState', () => {
  describe('Rendering', () => {
    it('should render heading and description', () => {
      // Arrange & Act
      render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert
      expect(screen.getByRole('heading', { name: /forge your first realm/i })).toBeInTheDocument();
      expect(screen.getByText(/every great saga/i)).toBeInTheDocument();
    });

    it('should render create realm button', () => {
      // Arrange & Act
      render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert
      const button = screen.getByRole('button', { name: /create realm/i });
      expect(button).toBeInTheDocument();
      expect(button).toBeEnabled();
    });

    it('should display decorative icon or illustration', () => {
      // Arrange & Act
      const { container } = render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert - icon should have aria-hidden="true" for decorative use
      const icon = container.querySelector('[aria-hidden="true"]');
      expect(icon).toBeInTheDocument();
    });
  });

  describe('Interactions', () => {
    it('should call onCreateWorld when button is clicked', async () => {
      // Arrange
      const user = userEvent.setup();
      const onCreateWorld = vi.fn();
      render(<EmptyState onCreateWorld={onCreateWorld} />);

      // Act
      const button = screen.getByRole('button', { name: /create realm/i });
      await user.click(button);

      // Assert
      expect(onCreateWorld).toHaveBeenCalledTimes(1);
    });

    it('should support keyboard activation (Enter key)', async () => {
      // Arrange
      const user = userEvent.setup();
      const onCreateWorld = vi.fn();
      render(<EmptyState onCreateWorld={onCreateWorld} />);

      // Act
      const button = screen.getByRole('button', { name: /create realm/i });
      button.focus();
      await user.keyboard('{Enter}');

      // Assert
      expect(onCreateWorld).toHaveBeenCalledTimes(1);
    });

    it('should support keyboard activation (Space key)', async () => {
      // Arrange
      const user = userEvent.setup();
      const onCreateWorld = vi.fn();
      render(<EmptyState onCreateWorld={onCreateWorld} />);

      // Act
      const button = screen.getByRole('button', { name: /create realm/i });
      button.focus();
      await user.keyboard(' ');

      // Assert
      expect(onCreateWorld).toHaveBeenCalledTimes(1);
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations', async () => {
      // Arrange & Act
      const { container } = render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have visible focus indicator on button', async () => {
      // Arrange
      const user = userEvent.setup();
      render(<EmptyState onCreateWorld={vi.fn()} />);

      // Act
      const button = screen.getByRole('button', { name: /create realm/i });
      await user.tab();

      // Assert
      expect(button).toHaveFocus();
    });

    it('should have appropriate heading level (h2 or h3)', () => {
      // Arrange & Act
      render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert
      const heading = screen.getByRole('heading', { name: /forge your first realm/i });
      expect(heading.tagName).toMatch(/^H[2-3]$/);
    });
  });

  describe('Content', () => {
    it('should provide clear call-to-action messaging', () => {
      // Arrange & Act
      render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert
      expect(screen.getByText(/forge your first realm/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /create realm/i })).toBeInTheDocument();
    });

    it('should explain what happens when user creates a world', () => {
      // Arrange & Act
      render(<EmptyState onCreateWorld={vi.fn()} />);

      // Assert
      // Should mention campaign building, world navigation, or similar context
      const description = screen.getByText(/every great saga/i);
      expect(description).toBeInTheDocument();
    });
  });
});
