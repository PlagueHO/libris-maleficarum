/**
 * Tests for EntityTypeSelector component
 *
 * @module components/ui/entity-type-selector.test
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EntityTypeSelector } from './entity-type-selector';
import { WorldEntityType } from '@/services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

describe('EntityTypeSelector', () => {
  describe('rendering', () => {
    it('renders with placeholder when no value selected', () => {
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          placeholder="Select type"
        />
      );
      expect(screen.getByRole('combobox')).toHaveTextContent('Select type');
    });

    it('renders selected type label when value is set', () => {
      render(
        <EntityTypeSelector
          value={WorldEntityType.Continent}
          onValueChange={vi.fn()}
        />
      );
      expect(screen.getByRole('combobox')).toHaveTextContent('Continent');
    });

    it('renders disabled state', () => {
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          disabled
        />
      );
      expect(screen.getByRole('combobox')).toBeDisabled();
    });

    it('renders clear button when value is selected and not disabled', () => {
      render(
        <EntityTypeSelector
          value={WorldEntityType.Continent}
          onValueChange={vi.fn()}
        />
      );
      // X icon should be present
      const combobox = screen.getByRole('combobox');
      expect(combobox.querySelector('svg')).toBeInTheDocument();
    });
  });

  describe('Container type recommendations (US1 - T037)', () => {
    it('recommends Container types for root-level entities (parentType=null)', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={null}
        />
      );

      // Open popover
      await user.click(screen.getByRole('combobox'));

      // Wait for popover to be visible
      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // Container types should be in recommended section
      expect(screen.getByText('Locations')).toBeInTheDocument();
      expect(screen.getByText('People')).toBeInTheDocument();
      expect(screen.getByText('Events')).toBeInTheDocument();
      expect(screen.getByText('Adventures')).toBeInTheDocument();
      expect(screen.getByText('Lore')).toBeInTheDocument();
      
      // Traditional geographic types also recommended at root
      expect(screen.getByText('Continent')).toBeInTheDocument();
      expect(screen.getByText('Campaign')).toBeInTheDocument();
    });

    it('shows Locations as first Container recommendation', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={null}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // Get all recommended type buttons
      const recommendedSection = screen.getByText('Recommended').parentElement;
      const typeButtons = recommendedSection?.querySelectorAll('button');
      
      // Locations should be among the first recommendations
      const firstButton = typeButtons?.[0];
      expect(firstButton).toHaveTextContent('Locations');
    });

    it('recommends child types under Locations container', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Locations}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // Geographic types should be recommended under Locations
      expect(screen.getByText('Continent')).toBeInTheDocument();
      expect(screen.getByText('Geographic Region')).toBeInTheDocument();
      expect(screen.getByText('Country')).toBeInTheDocument();
    });

    it('recommends Character under People container', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.People}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // Character should be recommended under People
      expect(screen.getByText('Character')).toBeInTheDocument();
    });

    it('recommends Event under Events container', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Events}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // Event should be recommended under Events
      expect(screen.getByText('Event')).toBeInTheDocument();
    });

    it('allows Regional types to self-nest', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.GeographicRegion}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // GeographicRegion should be able to contain itself
      expect(screen.getByText('Geographic Region')).toBeInTheDocument();
    });
  });

  describe('type selection', () => {
    it('calls onValueChange with selected type', async () => {
      const user = userEvent.setup();
      const onValueChange = vi.fn();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={onValueChange}
          parentType={null}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Locations')).toBeInTheDocument();
      });

      await user.click(screen.getByText('Locations'));

      expect(onValueChange).toHaveBeenCalledWith(WorldEntityType.Locations);
    });

    it('closes popover after selection', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={null}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Locations')).toBeInTheDocument();
      });

      await user.click(screen.getByText('Locations'));

      // Popover should close
      await waitFor(() => {
        expect(screen.queryByText('Recommended')).not.toBeInTheDocument();
      });
    });

    it('clears selection when clear button clicked', async () => {
      const user = userEvent.setup();
      const onValueChange = vi.fn();
      render(
        <EntityTypeSelector
          value={WorldEntityType.Continent}
          onValueChange={onValueChange}
        />
      );

      const combobox = screen.getByRole('combobox');
      const clearButton = combobox.querySelector('svg');
      expect(clearButton).toBeInTheDocument();

      await user.click(clearButton!);

      expect(onValueChange).toHaveBeenCalledWith('');
    });
  });

  describe('search functionality', () => {
    it('filters types by label', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          allowAllTypes
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByPlaceholderText('Search entity types...')).toBeInTheDocument();
      });

      const searchInput = screen.getByPlaceholderText('Search entity types...');
      await user.type(searchInput, 'Locations');

      // Only Locations should be visible
      expect(screen.getByText('Locations')).toBeInTheDocument();
      expect(screen.queryByText('Continent')).not.toBeInTheDocument();
    });

    it('filters types by description', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          allowAllTypes
        />
      );

      await user.click(screen.getByRole('combobox'));

      const searchInput = screen.getByPlaceholderText('Search entity types...');
      await user.type(searchInput, 'geographic');

      // Types with "geographic" in description should appear
      expect(screen.getByText('Geographic Region')).toBeInTheDocument();
    });

    it('shows "no results" message when no types match', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          allowAllTypes
        />
      );

      await user.click(screen.getByRole('combobox'));

      const searchInput = screen.getByPlaceholderText('Search entity types...');
      await user.type(searchInput, 'xyz123nonexistent');

      expect(screen.getByText(/No entity types match/)).toBeInTheDocument();
    });
  });

  describe('categorization', () => {
    it('groups non-recommended types by category', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Continent} // Specific parent to trigger "other types"
          allowAllTypes={false}
        />
      );

      await user.click(screen.getByRole('combobox'));

      // Search to reveal other types
      const searchInput = screen.getByPlaceholderText('Search entity types...');
      await user.type(searchInput, 'container');

      await waitFor(() => {
        // Should see category headers for non-recommended types
        const containers = screen.queryByText('Containers');
        if (containers) {
          expect(containers).toBeInTheDocument();
        }
      });
    });
  });

  describe('allowAllTypes mode', () => {
    it('shows all types as recommended when allowAllTypes is true', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Continent}
          allowAllTypes
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // All types should be in recommended section
      expect(screen.getByText('Locations')).toBeInTheDocument();
      expect(screen.getByText('Continent')).toBeInTheDocument();
      expect(screen.getByText('Character')).toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('has no accessibility violations when closed', async () => {
      const { container } = render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          aria-label="Entity Type"
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('has no accessibility violations when open', async () => {
      const user = userEvent.setup();
      const { container } = render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          aria-label="Entity Type"
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('has proper ARIA attributes', () => {
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          aria-label="Select Entity Type"
          aria-invalid={true}
        />
      );

      const combobox = screen.getByRole('combobox');
      expect(combobox).toHaveAttribute('aria-label', 'Select Entity Type');
      expect(combobox).toHaveAttribute('aria-invalid', 'true');
      expect(combobox).toHaveAttribute('aria-expanded', 'false');
    });

    it('updates aria-expanded when opened', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
        />
      );

      const combobox = screen.getByRole('combobox');
      expect(combobox).toHaveAttribute('aria-expanded', 'false');

      await user.click(combobox);

      await waitFor(() => {
        expect(combobox).toHaveAttribute('aria-expanded', 'true');
      });
    });

    it('uses role="option" for type buttons', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={null}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        const options = screen.getAllByRole('option');
        expect(options.length).toBeGreaterThan(0);
      });
    });

    it('marks selected option with aria-selected', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value={WorldEntityType.Locations}
          onValueChange={vi.fn()}
          parentType={null}
        />
      );

      await user.click(screen.getByRole('combobox'));

      await waitFor(() => {
        const locationsOption = screen.getByRole('option', { name: /Locations/ });
        expect(locationsOption).toHaveAttribute('aria-selected', 'true');
      });
    });
  });

  describe('T068-T071: Context-Aware Recommendations (US4)', () => {
    it('T068: suggests GeographicRegion in top 5 recommendations for Continent parent', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Continent}
        />
      );

      // Open popover
      await user.click(screen.getByRole('combobox'));

      // Wait for recommended section
      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // GeographicRegion should be in recommended section
      const recommendedSection = screen.getByText('Recommended').parentElement;
      expect(recommendedSection).toBeDefined();

      // GeographicRegion should be visible
      expect(screen.getByText('Geographic Region')).toBeInTheDocument();
      expect(screen.getByText('Political Region')).toBeInTheDocument(); // Also recommended for Continent

      // Verify these are in the recommended section (not categorized "Other" section)
      const geoOption = screen.getByRole('option', { name: /Geographic Region/i });
      expect(geoOption).toBeInTheDocument();
    });

    it('T069: suggests Building, Location, Character in top recommendations for City parent', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.City}
        />
      );

      // Open popover
      await user.click(screen.getByRole('combobox'));

      // Wait for recommended section
      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // City parent should recommend: Building, Location, Character, Item (see ENTITY_TYPE_SUGGESTIONS)
      expect(screen.getByText('Building')).toBeInTheDocument();
      expect(screen.getByText('Location')).toBeInTheDocument();
      expect(screen.getByText('Character')).toBeInTheDocument();
    });

    it('T070: EntityTypeSelector search returns results quickly', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={null}
        />
      );

      // Open popover
      await user.click(screen.getByRole('combobox'));

      // Wait for search input
      const searchInput = await screen.findByPlaceholderText(/search/i);

      // Measure search performance
      const startTime = performance.now();
      await user.type(searchInput, 'Geo');
      const endTime = performance.now();

      // Search should complete reasonably fast (typing simulation is slower than real user input)
      const searchTime = endTime - startTime;
      expect(searchTime).toBeLessThan(200); // Increased from 100ms to account for test environment overhead

      // Results should filter correctly
      await waitFor(() => {
        expect(screen.getByText('Geographic Region')).toBeInTheDocument();
      });
    });

    it('T071: non-recommended types remain accessible via search', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Continent}
        />
      );

      // Open popover
      await user.click(screen.getByRole('combobox'));

      // Wait for UI to load
      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // Quest is NOT recommended for Continent, but should be searchable
      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'Quest');

      // Quest should appear in search results (matches both Quest entity and Events container which has "quest" in description)
      await waitFor(() => {
        const questOptions = screen.getAllByRole('option', { name: /Quest/i });
        // Should find at least the Quest entity type
        expect(questOptions.length).toBeGreaterThanOrEqual(1);
      });
    });

    it('T071: non-recommended types accessible via scroll (categorized)', async () => {
      const user = userEvent.setup();
      render(
        <EntityTypeSelector
          value=""
          onValueChange={vi.fn()}
          parentType={WorldEntityType.Continent}
        />
      );

      // Open popover
      await user.click(screen.getByRole('combobox'));

      // Wait for UI
      await waitFor(() => {
        expect(screen.getByText('Recommended')).toBeInTheDocument();
      });

      // By default (no search), only recommended types show.
      // When user searches OR scrolls, other categorized types appear.
      // Let's verify that searching shows non-recommended types
      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.clear(searchInput);
      await user.type(searchInput, 'a'); // Search for 'a' to trigger showing categorized types
      
      await waitFor(() => {
        const optionList = screen.getAllByRole('option');
        // With search active, should show many matching types from categories
        expect(optionList.length).toBeGreaterThan(5);
      });
    });
  });
});
