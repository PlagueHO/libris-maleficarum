/**
 * EntityDetailReadOnlyView Component Tests
 *
 * Tests for read-only entity detail display with Edit button.
 * Covers rendering, interactions, accessibility, and edge cases.
 *
 * @see EntityDetailReadOnlyView.tsx
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EntityDetailReadOnlyView } from './EntityDetailReadOnlyView';
import type { WorldEntity } from '@/services/types/worldEntity.types';
import { WorldEntityType } from '@/services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

// Mock entity data
const mockEntity: WorldEntity = {
  id: 'entity-1',
  worldId: 'world-1',
  parentId: null,
  entityType: WorldEntityType.Continent,
  name: 'Faerûn',
  description: 'The main continent of Toril, home to many diverse cultures and civilizations.',
  tags: ['primary', 'explored'],
  properties: undefined,
  path: ['/Faerûn'],
  depth: 0,
  hasChildren: true,
  ownerId: 'user-1',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  isDeleted: false,
  schemaVersion: 1,
};

const mockEntityWithProperties: WorldEntity = {
  ...mockEntity,
  id: 'entity-2',
  name: 'Sword Coast',
  entityType: WorldEntityType.GeographicRegion,
  properties: JSON.stringify({
    climate: 'Temperate',
    terrain: 'Coastal plains and hills',
    population: '2.5 million',
  }),
};

const mockEntityNoDescription: WorldEntity = {
  ...mockEntity,
  id: 'entity-3',
  name: 'Unknown Region',
  description: '',
  tags: [],
};

describe('EntityDetailReadOnlyView (T022)', () => {
  const mockOnEditClick = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering (T022)', () => {
    it('should render entity name as h1', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      const heading = screen.getByRole('heading', { level: 1, name: /faerûn/i });
      expect(heading).toBeInTheDocument();
    });

    it('should display entity type badge', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      expect(screen.getByText('Continent')).toBeInTheDocument();
    });

    it('should display all tags as badges', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      expect(screen.getByText('primary')).toBeInTheDocument();
      expect(screen.getByText('explored')).toBeInTheDocument();
    });

    it('should render description with preserved whitespace', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      expect(screen.getByText(/the main continent of toril/i)).toBeInTheDocument();
    });

    it('should show "No description" message when description is empty', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntityNoDescription} onEditClick={mockOnEditClick} />);

      // Assert
      expect(screen.getByText(/no description available/i)).toBeInTheDocument();
    });

    it('should display Edit button in top-right corner', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      const editButton = screen.getByRole('button', { name: /edit/i });
      expect(editButton).toBeInTheDocument();
    });

    it('should render custom properties if present', () => {
      // Arrange & Act
      render(
        <EntityDetailReadOnlyView entity={mockEntityWithProperties} onEditClick={mockOnEditClick} />
      );

      // Assert - custom properties should be displayed
      expect(screen.getByText(/climate/i)).toBeInTheDocument();
      expect(screen.getByText(/temperate/i)).toBeInTheDocument();
    });

    it('should not render custom properties section when properties is null', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert - custom properties section should not exist
      expect(screen.queryByText(/custom properties/i)).not.toBeInTheDocument();
    });
  });

  describe('Interaction (T022)', () => {
    it('should call onEditClick when Edit button clicked', async () => {
      // Arrange
      const user = userEvent.setup();

      // Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      await user.click(editButton);

      // Assert
      expect(mockOnEditClick).toHaveBeenCalledTimes(1);
    });

    it('should call onEditClick on Enter key when button focused', async () => {
      // Arrange
      const user = userEvent.setup();

      // Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      editButton.focus();
      await user.keyboard('{Enter}');

      // Assert
      expect(mockOnEditClick).toHaveBeenCalledTimes(1);
    });

    it('should call onEditClick on Space key when button focused', async () => {
      // Arrange
      const user = userEvent.setup();

      // Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      editButton.focus();
      await user.keyboard(' ');

      // Assert
      expect(mockOnEditClick).toHaveBeenCalledTimes(1);
    });

    it('should disable Edit button when disableEdit=true', () => {
      // Arrange & Act
      render(
        <EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} disableEdit={true} />
      );

      // Assert
      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      expect(editButton).toBeDisabled();
    });

    it('should not call onEditClick when button is disabled', async () => {
      // Arrange
      const user = userEvent.setup();

      // Act
      render(
        <EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} disableEdit={true} />
      );

      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      await user.click(editButton);

      // Assert
      expect(mockOnEditClick).not.toHaveBeenCalled();
    });
  });

  describe('Accessibility (T022)', () => {
    it('should have no accessibility violations', async () => {
      // Arrange & Act
      const { container } = render(
        <EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />
      );

      // Assert
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have entity name as heading with level 1', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      const heading = screen.getByRole('heading', { level: 1 });
      expect(heading).toHaveTextContent('Faerûn');
    });

    it('should have Edit button with accessible label', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      const editButton = screen.getByRole('button', { name: 'Edit Faerûn' });
      expect(editButton).toHaveAttribute('aria-label', 'Edit Faerûn');
    });

    it('should make Edit button keyboard-focusable', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntity} onEditClick={mockOnEditClick} />);

      // Assert
      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      expect(editButton).toHaveAttribute('tabindex', '0');
    });
  });

  describe('Edge Cases (T022)', () => {
    it('should handle entity with no tags', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntityNoDescription} onEditClick={mockOnEditClick} />);

      // Assert - should render without errors and show entity name
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
    });

    it('should handle entity with null description', () => {
      // Arrange
      const entityNullDesc: WorldEntity = {
        ...mockEntity,
        description: null as unknown as string,
      };

      // Act
      render(<EntityDetailReadOnlyView entity={entityNullDesc} onEditClick={mockOnEditClick} />);

      // Assert
      expect(screen.getByText(/no description available/i)).toBeInTheDocument();
    });

    it('should handle entity with empty string description', () => {
      // Arrange & Act
      render(<EntityDetailReadOnlyView entity={mockEntityNoDescription} onEditClick={mockOnEditClick} />);

      // Assert
      expect(screen.getByText(/no description available/i)).toBeInTheDocument();
    });

    it('should handle entity with very long name', () => {
      // Arrange
      const longNameEntity: WorldEntity = {
        ...mockEntity,
        name: 'A'.repeat(150),
      };

      // Act
      render(<EntityDetailReadOnlyView entity={longNameEntity} onEditClick={mockOnEditClick} />);

      // Assert - should render without errors
      expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
    });

    it('should handle entity with malformed custom properties', () => {
      // Arrange
      const malformedEntity: WorldEntity = {
        ...mockEntity,
        properties: 'invalid JSON',
      };

      // Act & Assert - should not crash
      expect(() => {
        render(<EntityDetailReadOnlyView entity={malformedEntity} onEditClick={mockOnEditClick} />);
      }).not.toThrow();
    });
  });

  describe('Custom Properties Display (T028)', () => {
    it('should display custom properties as key-value pairs using Object.entries()', () => {
      // Arrange & Act
      render(
        <EntityDetailReadOnlyView entity={mockEntityWithProperties} onEditClick={mockOnEditClick} />
      );

      // Assert - all properties should be visible
      expect(screen.getByText(/climate/i)).toBeInTheDocument();
      expect(screen.getByText(/temperate/i)).toBeInTheDocument();
      expect(screen.getByText(/terrain/i)).toBeInTheDocument();
      expect(screen.getByText(/coastal plains and hills/i)).toBeInTheDocument();
      expect(screen.getByText(/population/i)).toBeInTheDocument();
      expect(screen.getByText(/2.5 million/i)).toBeInTheDocument();
    });

    it('should handle custom properties with nested objects', () => {
      // Arrange
      const nestedPropsEntity: WorldEntity = {
        ...mockEntity,
        properties: JSON.stringify({
          geography: {
            climate: 'Temperate',
            terrain: 'Coastal',
          },
        }),
      };

      // Act & Assert - should render without crashing
      expect(() => {
        render(<EntityDetailReadOnlyView entity={nestedPropsEntity} onEditClick={mockOnEditClick} />);
      }).not.toThrow();
    });
  });
});
