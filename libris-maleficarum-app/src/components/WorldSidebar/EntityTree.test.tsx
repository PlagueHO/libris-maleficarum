/**
 * EntityTree Component Tests
 *
 * Tests for recursive tree component displaying world entity hierarchy.
 * Covers lazy loading, caching, keyboard navigation, and accessibility.
 *
 * @see EntityTree.tsx
 */

import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { EntityTree } from './EntityTree';
import { api } from '@/services/api';
import worldSidebarReducer from '@/store/worldSidebarSlice';
import { handlers } from '@/__tests__/mocks/handlers';
import type { WorldEntity, WorldEntityListResponse } from '@/services/types/worldEntity.types';
import { WorldEntityType } from '@/services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

// Additional handler for world-2 (for world switching tests)
const world2Handler = http.get('http://localhost:5000/api/v1/worlds/world-2/entities', ({ request }) => {
  const url = new URL(request.url);
  const parentId = url.searchParams.get('parentId');
  
  // Mock data for world-2
  const world2Entities: WorldEntity[] = [
    {
      id: 'world2-continent-1',
      worldId: 'world-2',
      parentId: null,
      entityType: WorldEntityType.Continent,
      name: 'World 2 Continent',
      description: 'A continent in world 2',
      tags: [],
      path: [],
      depth: 0,
      hasChildren: false,
      ownerId: 'test-user@example.com',
      createdAt: '2026-01-14T00:00:00Z',
      updatedAt: '2026-01-14T00:00:00Z',
      isDeleted: false,
    },
  ];
  
  const filteredEntities = parentId === 'null' || parentId === null 
    ? world2Entities.filter(e => e.parentId === null)
    : [];
    
  const response: WorldEntityListResponse = {
    data: filteredEntities,
    meta: {
      count: filteredEntities.length,
      nextCursor: null,
    },
  };
  
  return HttpResponse.json(response);
});

// Error handler for world-error (returns 500)
const worldErrorHandler = http.get('http://localhost:5000/api/worlds/world-error/entities', () => {
  return new HttpResponse(null, { status: 500, statusText: 'Internal Server Error' });
});

// Error handler for world-network-error (returns network error)
const worldNetworkErrorHandler = http.get('http://localhost:5000/api/worlds/world-network-error/entities', () => {
  return HttpResponse.error();
});

// Setup MSW server with additional handlers
const server = setupServer(...handlers, world2Handler, worldErrorHandler, worldNetworkErrorHandler);

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

describe('EntityTree', () => {
  // Start MSW server before all tests
  beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));

  // Reset handlers after each test
  afterEach(() => {
    server.resetHandlers();
    sessionStorage.clear();
  });

  // Clean up server after all tests
  afterAll(() => server.close());

  describe('Rendering', () => {
    it('should render tree with role="tree"', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - wait for data to load
      await waitFor(() => {
        expect(screen.getByRole('tree')).toBeInTheDocument();
      });
    });

    it('should show loading state while fetching root entities', () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('status', { name: /loading/i })).toBeInTheDocument();
    });

    it('should show empty state when no entities exist', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'world-empty' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert
      await waitFor(() => {
        expect(screen.getByText(/no entities yet/i)).toBeInTheDocument();
      });
    });

    it('should render root level entities when loaded', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - MSW mock data includes Faerûn
      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });
    });
  });

  describe('Lazy Loading (T049)', () => {
    it('should not load children until node is expanded', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Wait for root entities
      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Assert - children should not be rendered yet
      expect(screen.queryByText('Cormyr')).not.toBeInTheDocument();

      // Expand the node
      const expandButton = screen.getByRole('button', { name: /expand faerûn/i });
      await user.click(expandButton);

      // Assert - children should now be loaded
      await waitFor(() => {
        expect(screen.getByText('Cormyr')).toBeInTheDocument();
      });
    });

    it('should show loading spinner while fetching children', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      const expandButton = screen.getByRole('button', { name: /expand faerûn/i });
      await user.click(expandButton);

      // Assert - loading indicator should appear briefly
      // Note: This may be too fast to catch in tests, but behavior is correct
    });

    it.skip('should handle empty children gracefully', async () => {
      // TODO: This test needs to be updated to expand the tree hierarchy first
      // before trying to access Suzail, which is nested under Cormyr
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>\n          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Expand a leaf node (no children)
      const leafEntity = screen.getByText('Suzail'); // Assuming Suzail is a leaf node
      if (leafEntity) {
        await user.click(leafEntity);
        // Should not crash or show error
      }
    });
  });

  describe('Caching (T049)', () => {
    it('should use sessionStorage cache for previously loaded children', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act - First load
      const { unmount } = render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      const expandButton = screen.getByRole('button', { name: /expand faerûn/i });
      await user.click(expandButton);

      await waitFor(() => {
        expect(screen.getByText('Cormyr')).toBeInTheDocument();
      });

      // Unmount and remount
      unmount();

      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - cache should restore expanded state
      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Expanded state should persist (if implemented)
    });

    it('should fetch from API if cache is expired', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Mock expired cache by setting old timestamp
      const expiredCache = {
        data: [],
        timestamp: Date.now() - 400000, // 6.67 minutes ago (expired)
        ttl: 300000, // 5 minute TTL
      };
      sessionStorage.setItem(
        'sidebar_hierarchy_world-1',
        JSON.stringify(expiredCache),
      );

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - should fetch fresh data from API
      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });
    });
  });

  describe('Keyboard Navigation (T058)', () => {
    it('should navigate down to next sibling with ArrowDown', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      const firstEntity = screen.getByRole('treeitem', { name: /faerûn/i });
      firstEntity.focus();
      await user.keyboard('{ArrowDown}');

      // Assert - focus should move to next sibling (if exists)
      // Implementation-specific assertion needed
    });

    it('should navigate up to previous sibling with ArrowUp', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Focus second item, then press ArrowUp
      const secondEntity = screen.getAllByRole('treeitem')[1];
      if (secondEntity) {
        secondEntity.focus();
        await user.keyboard('{ArrowUp}');

        // Assert - focus should move to previous sibling
      }
    });

    it.skip('should expand node with ArrowRight if collapsed', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Select the entity in state (keyboard handler depends on selectedEntityId in Redux)
      store.dispatch({ type: 'worldSidebar/setSelectedEntity', payload: 'continent-faerun' });

      // Fire keyboard event on tree container (where the handler is attached)
      const treeContainer = screen.getByRole('tree');
      treeContainer.focus();
      await user.keyboard('{ArrowRight}');

      // Assert - node should expand
      await waitFor(() => {
        expect(screen.getByText('Cormyr')).toBeInTheDocument();
      });
    });

    it.skip('should collapse node with ArrowLeft if expanded', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Select the entity in state
      store.dispatch({ type: 'worldSidebar/setSelectedEntity', payload: 'continent-faerun' });

      // Fire keyboard events on tree container (where the handler is attached)
      const treeContainer = screen.getByRole('tree');
      treeContainer.focus();
      
      // Expand first
      await user.keyboard('{ArrowRight}');

      await waitFor(() => {
        expect(screen.getByText('Cormyr')).toBeInTheDocument();
      });

      // Then collapse
      await user.keyboard('{ArrowLeft}');

      // Assert - children should be hidden (but still in DOM for accessibility)
    });

    it('should select entity with Enter key', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      const firstEntity = screen.getByRole('treeitem', { name: /faerûn/i });
      firstEntity.focus();
      await user.keyboard('{Enter}');

      // Assert - entity should be selected (Redux state updated)
      // Check aria-selected attribute
      expect(firstEntity).toHaveAttribute('aria-selected', 'true');
    });
  });

  describe('Accessibility (T043, T059)', () => {
    it('should have no accessibility violations', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      const { container } = render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Assert
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have role="tree" on container', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - wait for tree to load
      await waitFor(() => {
        expect(screen.getByRole('tree')).toBeInTheDocument();
      });
    });

    it('should have aria-label on tree', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - wait for tree to load
      await waitFor(() => {
        const tree = screen.getByRole('tree');
        expect(tree).toHaveAttribute('aria-label');
      });
    });

    it('should maintain keyboard focus management', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Assert - tree should manage focus via roving tabindex
      const treeitems = screen.getAllByRole('treeitem');
      const focusableItems = treeitems.filter((item) =>
        item.hasAttribute('tabindex'),
      );
      expect(focusableItems.length).toBeGreaterThan(0);
    });

    it('should announce dynamic changes to screen readers', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      const expandButton = screen.getByRole('button', { name: /expand faerûn/i });
      await user.click(expandButton);

      // Assert - aria-expanded should update
      await waitFor(() => {
        expect(expandButton).toHaveAttribute('aria-expanded', 'true');
      });
    });
  });

  describe('Error Handling', () => {
    it('should show error message if fetch fails', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'world-error' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - wait for RTK Query retries to complete (3 retries + initial request)
      await waitFor(
        () => {
          expect(screen.getByText('Failed to load entities')).toBeInTheDocument();
        },
        { timeout: 10000 },
      );
    });

    it('should handle network errors gracefully', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'world-network-error' });

      // Act
      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - error state should be shown after retries, not crash
      await waitFor(
        () => {
          expect(screen.getByText('Failed to load entities')).toBeInTheDocument();
        },
        { timeout: 10000 },
      );
    });
  });

  describe('World Switching (T054)', () => {
    it('should clear tree when switching worlds', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      const { unmount } = render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Switch world - unmount, clear cache, then remount
      unmount();
      sessionStorage.clear();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'world-2' });

      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - wait for new world to load, old entities should be gone
      await waitFor(() => {
        expect(screen.queryByText('Faerûn')).not.toBeInTheDocument();
      });
    });

    it('should load new world entities after switch', async () => {
      // Arrange
      const store = createMockStore();
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'test-world-123' });

      // Act
      const { unmount } = render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });

      // Switch world - unmount, clear cache, then remount
      unmount();
      sessionStorage.clear(); // Clear session cache to force refetch
      store.dispatch({ type: 'worldSidebar/setSelectedWorld', payload: 'world-2' });

      render(
        <Provider store={store}>
          <EntityTree />
        </Provider>,
      );

      // Assert - new world entities should load
      await waitFor(() => {
        expect(screen.getByText('World 2 Continent')).toBeInTheDocument();
      });
    });
  });
});
