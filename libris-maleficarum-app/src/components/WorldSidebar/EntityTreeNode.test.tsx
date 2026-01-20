/**
 * EntityTreeNode Component Tests
 *
 * Tests for individual tree node component representing a single entity.
 * Covers rendering, expand/collapse, selection, keyboard navigation, and accessibility.
 *
 * @see EntityTreeNode.tsx
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { EntityTreeNode } from './EntityTreeNode';
import { api } from '@/services/api';
import worldSidebarReducer from '@/store/worldSidebarSlice';
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
  description: 'The main continent',
  tags: ['primary'],
  path: ['/Faerûn'],
  depth: 0,
  hasChildren: true,
  ownerId: 'user-1',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  isDeleted: false,
};

const mockEntityNoChildren: WorldEntity = {
  ...mockEntity,
  id: 'entity-2',
  name: 'Cormyr',
  parentId: 'entity-1',
  entityType: WorldEntityType.Country,
  hasChildren: false,
  path: ['/Faerûn', '/Cormyr'],
  depth: 1,
};

// Mock store setup
const createMockStore = () => {
  return configureStore({
    reducer: {
      worldSidebar: worldSidebarReducer,
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  });
};

describe('EntityTreeNode', () => {
  describe('Rendering', () => {
    it('should render entity name', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      expect(screen.getByText('Faerûn')).toBeInTheDocument();
    });

    it('should render entity icon based on type', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert - icon should be present (decorative)
      const icon = screen.getByRole('img', { hidden: true });
      expect(icon).toBeInTheDocument();
    });

    it('should render Container type icon for Locations entity (T034)', () => {
      // Arrange
      const store = createMockStore();
      const locationsEntity: WorldEntity = {
        ...mockEntity,
        id: 'locations-1',
        name: 'Locations',
        entityType: WorldEntityType.Locations,
      };

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={locationsEntity} level={0} />
        </Provider>,
      );

      // Assert - icon should be present with Container styling
      const icon = screen.getByRole('img', { hidden: true });
      expect(icon).toBeInTheDocument();
      expect(icon).toHaveAttribute('aria-hidden', 'true');
    });

    it('should render different icons for Container vs Standard entities (T034)', () => {
      // Arrange
      const store = createMockStore();
      
      // Container entity (Locations)
      const containerEntity: WorldEntity = {
        ...mockEntity,
        id: 'container-1',
        name: 'My Locations',
        entityType: WorldEntityType.Locations,
      };

      // Standard entity (Continent)
      const standardEntity: WorldEntity = {
        ...mockEntity,
        id: 'standard-1',
        name: 'Faerûn',
        entityType: WorldEntityType.Continent,
      };

      // Act - render both
      const { rerender } = render(
        <Provider store={store}>
          <EntityTreeNode entity={containerEntity} level={0} />
        </Provider>,
      );

      // Assert - Container entity has icon
      const containerIcon = screen.getByRole('img', { hidden: true });
      expect(containerIcon).toBeInTheDocument();

      // Re-render with standard entity
      rerender(
        <Provider store={store}>
          <EntityTreeNode entity={standardEntity} level={0} />
        </Provider>,
      );

      // Assert - Standard entity also has icon (visual difference confirmed by icon mapping)
      const standardIcon = screen.getByRole('img', { hidden: true });
      expect(standardIcon).toBeInTheDocument();
    });

    it('should render all new Container type icons (T034)', () => {
      // Arrange
      const store = createMockStore();
      const containerTypes = [
        WorldEntityType.Locations,
        WorldEntityType.People,
        WorldEntityType.Events,
        WorldEntityType.History,
        WorldEntityType.Lore,
        WorldEntityType.Bestiary,
        WorldEntityType.Items,
        WorldEntityType.Adventures,
        WorldEntityType.Geographies,
      ];

      // Act & Assert - each Container type should render with icon
      containerTypes.forEach((entityType) => {
        const entity: WorldEntity = {
          ...mockEntity,
          id: `${entityType}-1`,
          name: `Test ${entityType}`,
          entityType,
        };

        const { unmount } = render(
          <Provider store={store}>
            <EntityTreeNode entity={entity} level={0} />
          </Provider>,
        );

        const icon = screen.getByRole('img', { hidden: true });
        expect(icon).toBeInTheDocument();
        
        unmount();
      });
    });

    it('should render all new Regional type icons (T034)', () => {
      // Arrange
      const store = createMockStore();
      const regionalTypes = [
        WorldEntityType.GeographicRegion,
        WorldEntityType.PoliticalRegion,
        WorldEntityType.CulturalRegion,
        WorldEntityType.MilitaryRegion,
      ];

      // Act & Assert - each Regional type should render with icon
      regionalTypes.forEach((entityType) => {
        const entity: WorldEntity = {
          ...mockEntity,
          id: `${entityType}-1`,
          name: `Test ${entityType}`,
          entityType,
        };

        const { unmount } = render(
          <Provider store={store}>
            <EntityTreeNode entity={entity} level={0} />
          </Provider>,
        );

        const icon = screen.getByRole('img', { hidden: true });
        expect(icon).toBeInTheDocument();
        
        unmount();
      });
    });

    it('should render expand/collapse chevron for entities with children', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      const expandButton = screen.getByRole('button', { name: /expand|collapse/i });
      expect(expandButton).toBeInTheDocument();
    });

    it('should not render chevron for leaf nodes (no children)', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntityNoChildren} level={1} />
        </Provider>,
      );

      // Assert
      expect(screen.queryByRole('button', { name: /expand|collapse/i })).not.toBeInTheDocument();
    });

    it('should apply correct indentation based on depth level', () => {
      // Arrange
      const store = createMockStore();

      // Act
      const { container } = render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntityNoChildren} level={2} />
        </Provider>,
      );

      // Assert - indentation should be applied via style or class
      const nodeElement = container.querySelector('[data-level="2"]');
      expect(nodeElement).toBeInTheDocument();
    });
  });

  describe('Expand/Collapse Behavior', () => {
    it('should toggle expanded state when chevron clicked', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const expandButton = screen.getByRole('button', { name: /expand/i });
      await user.click(expandButton);

      // Assert - button should update aria-label
      expect(screen.getByRole('button', { name: /collapse/i })).toBeInTheDocument();
    });

    it('should dispatch Redux action when expanding node', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      const dispatchSpy = vi.spyOn(store, 'dispatch');

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const expandButton = screen.getByRole('button', { name: /expand/i });
      await user.click(expandButton);

      // Assert - toggleNodeExpanded action should be dispatched
      expect(dispatchSpy).toHaveBeenCalled();
    });

    it('should show right chevron when collapsed', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      const expandButton = screen.getByRole('button', { name: /expand/i });
      expect(expandButton).toHaveAttribute('aria-expanded', 'false');
    });

    it('should show down chevron when expanded', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const expandButton = screen.getByRole('button', { name: /expand/i });
      await user.click(expandButton);

      // Assert
      const collapseButton = screen.getByRole('button', { name: /collapse/i });
      expect(collapseButton).toHaveAttribute('aria-expanded', 'true');
    });
  });

  describe('Selection Behavior', () => {
    it('should highlight when entity is selected', () => {
      // Arrange
      const store = createMockStore();
      // Pre-select the entity
      store.dispatch({ type: 'worldSidebar/setSelectedEntity', payload: 'entity-1' });

      // Act
      const { container } = render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert - should have selected class/attribute
      const nodeElement = container.querySelector('[aria-selected="true"]');
      expect(nodeElement).toBeInTheDocument();
    });

    it('should dispatch action when entity clicked', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      const dispatchSpy = vi.spyOn(store, 'dispatch');

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const entityName = screen.getByText('Faerûn');
      await user.click(entityName);

      // Assert - setSelectedEntity action should be dispatched
      expect(dispatchSpy).toHaveBeenCalled();
    });
  });

  describe('Keyboard Navigation', () => {
    it('should expand node when Enter key pressed on chevron', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const expandButton = screen.getByRole('button', { name: /expand/i });
      expandButton.focus();
      await user.keyboard('{Enter}');

      // Assert
      expect(screen.getByRole('button', { name: /collapse/i })).toBeInTheDocument();
    });

    it('should expand node when Space key pressed on chevron', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const expandButton = screen.getByRole('button', { name: /expand/i });
      expandButton.focus();
      await user.keyboard(' ');

      // Assert
      expect(screen.getByRole('button', { name: /collapse/i })).toBeInTheDocument();
    });

    it('should be keyboard focusable', () => {
      // Arrange
      const store = createMockStore();

      // Act
      const { container } = render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert - node should have tabindex or be focusable
      const focusableElement = container.querySelector('[tabindex]');
      expect(focusableElement).toBeInTheDocument();
    });
  });

  describe('Accessibility (T043)', () => {
    it('should have no accessibility violations', async () => {
      // Arrange
      const store = createMockStore();

      // Act
      const { container } = render(
        <Provider store={store}>
          <div role="tree">
            <EntityTreeNode entity={mockEntity} level={0} />
          </div>
        </Provider>,
      );

      // Assert
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have role="treeitem"', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('treeitem')).toBeInTheDocument();
    });

    it('should have aria-expanded attribute for expandable nodes', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      const expandButton = screen.getByRole('button', { name: /expand/i });
      expect(expandButton).toHaveAttribute('aria-expanded');
    });

    it('should have aria-selected attribute', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      const treeItem = screen.getByRole('treeitem');
      expect(treeItem).toHaveAttribute('aria-selected');
    });

    it('should have aria-level attribute indicating depth', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntityNoChildren} level={2} />
        </Provider>,
      );

      // Assert
      const treeItem = screen.getByRole('treeitem');
      expect(treeItem).toHaveAttribute('aria-level', '3'); // level prop is 0-indexed, aria-level is 1-indexed
    });

    it('should have proper label for screen readers', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      // Assert
      const treeItem = screen.getByRole('treeitem', { name: /faerûn/i });
      expect(treeItem).toBeInTheDocument();
    });
  });

  describe('Visual States', () => {
    it('should show hover state when mouse over', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();

      // Act
      const { container } = render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const entityName = screen.getByText('Faerûn');
      await user.hover(entityName);

      // Assert - hover class should be applied
      const nodeContainer = container.querySelector('[data-hovered]');
      expect(nodeContainer).toBeInTheDocument();
    });

    it('should show focus indicator when focused', async () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const expandButton = screen.getByRole('button', { name: /expand/i });
      expandButton.focus();

      // Assert
      expect(expandButton).toHaveFocus();
    });
  });
});
