/**
 * Unit tests for DynamicPropertiesView component
 *
 * Tests schema-driven read-only property display including:
 * - Schema-based rendering for all entity types
 * - Numeric formatting with thousand separators
 * - TagArray rendering as badges
 * - Fallback to Object.entries() when schema missing
 * - Accessibility compliance
 *
 * @module __tests__/DynamicPropertiesView.test
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { DynamicPropertiesView } from '../DynamicPropertiesView';
import { WorldEntityType } from '@/services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

describe('T036: DynamicPropertiesView - Schema-Based Rendering', () => {
  describe('Geographic Region', () => {
    it('should render all Geographic Region properties with proper formatting', () => {
      const properties = {
        Climate: 'Tropical monsoon climate',
        Terrain: 'Dense rainforest\nwith river valleys',
        Population: 1500000,
        Area: 2500.75,
      };

      render(
        <DynamicPropertiesView
          entityType={WorldEntityType.GeographicRegion}
          value={properties}
        />
      );

      // Section header
      expect(screen.getByText('Geographic Region Properties')).toBeInTheDocument();

      // Text fields
      expect(screen.getByText('Climate')).toBeInTheDocument();
      expect(screen.getByText('Tropical monsoon climate')).toBeInTheDocument();

      // Multiline textarea field
      expect(screen.getByText('Terrain')).toBeInTheDocument();
      expect(screen.getByText(/Dense rainforest\s+with river valleys/)).toBeInTheDocument();

      // Integer with thousand separators (T033)
      expect(screen.getByText('Population')).toBeInTheDocument();
      expect(screen.getByText('1,500,000')).toBeInTheDocument();

      // Decimal with thousand separators (T033)
      expect(screen.getByText('Area (sq km)')).toBeInTheDocument();
      expect(screen.getByText('2,500.75')).toBeInTheDocument();
    });

    it('should handle large numbers with proper formatting', () => {
      const properties = {
        Population: 123456789,
        Area: 987654.321,
      };

      render(
        <DynamicPropertiesView
          entityType={WorldEntityType.GeographicRegion}
          value={properties}
        />
      );

      expect(screen.getByText('123,456,789')).toBeInTheDocument();
      expect(screen.getByText('987,654.321')).toBeInTheDocument();
    });
  });

  describe('Political Region', () => {
    it('should render Political Region properties with tagArray as badges', () => {
      const properties = {
        GovernmentType: 'Federal republic\nwith parliamentary democracy',
        MemberStates: ['State A', 'State B', 'State C'],
        EstablishedDate: '1776-07-04',
      };

      render(
        <DynamicPropertiesView
          entityType={WorldEntityType.PoliticalRegion}
          value={properties}
        />
      );

      // Section header
      expect(screen.getByText('Political Region Properties')).toBeInTheDocument();

      // Multiline textarea
      expect(screen.getByText(/Federal republic\s+with parliamentary democracy/)).toBeInTheDocument();

      // T034: TagArray as badges
      expect(screen.getByText('Member States')).toBeInTheDocument();
      expect(screen.getByText('State A')).toBeInTheDocument();
      expect(screen.getByText('State B')).toBeInTheDocument();
      expect(screen.getByText('State C')).toBeInTheDocument();

      // Text field
      expect(screen.getByText('Established Date')).toBeInTheDocument();
      expect(screen.getByText('1776-07-04')).toBeInTheDocument();
    });

    it('should handle empty tagArray gracefully', () => {
      const properties = {
        GovernmentType: 'Monarchy',
        MemberStates: [],
        EstablishedDate: '1000-01-01',
      };

      render(
        <DynamicPropertiesView
          entityType={WorldEntityType.PoliticalRegion}
          value={properties}
        />
      );

      expect(screen.getByText('Member States')).toBeInTheDocument();
      expect(screen.getByText('No items')).toBeInTheDocument();
    });
  });

  describe('Cultural Region', () => {
    it('should render Cultural Region properties with multiple tagArrays', () => {
      const properties = {
        Languages: ['English', 'Spanish', 'French'],
        Religions: ['Christianity', 'Islam'],
        CulturalTraits: 'Rich artistic heritage\nStrong oral traditions',
      };

      render(
        <DynamicPropertiesView
          entityType={WorldEntityType.CulturalRegion}
          value={properties}
        />
      );

      // Section header
      expect(screen.getByText('Cultural Region Properties')).toBeInTheDocument();

      // First tagArray
      expect(screen.getByText('Languages')).toBeInTheDocument();
      expect(screen.getByText('English')).toBeInTheDocument();
      expect(screen.getByText('Spanish')).toBeInTheDocument();
      expect(screen.getByText('French')).toBeInTheDocument();

      // Second tagArray
      expect(screen.getByText('Religions')).toBeInTheDocument();
      expect(screen.getByText('Christianity')).toBeInTheDocument();
      expect(screen.getByText('Islam')).toBeInTheDocument();

      // Multiline textarea
      expect(screen.getByText(/Rich artistic heritage\s+Strong oral traditions/)).toBeInTheDocument();
    });
  });

  describe('Military Region', () => {
    it('should render Military Region properties', () => {
      const properties = {
        CommandStructure: 'Unified command\nUnder central authority',
        StrategicImportance: 'Critical defensive position\nControls key trade routes',
        MilitaryAssets: ['Fort Alpha', 'Naval Base Beta', 'Airfield Gamma'],
      };

      render(
        <DynamicPropertiesView
          entityType={WorldEntityType.MilitaryRegion}
          value={properties}
        />
      );

      // Section header
      expect(screen.getByText('Military Region Properties')).toBeInTheDocument();

      // Multiline textareas
      expect(screen.getByText(/Unified command\s+Under central authority/)).toBeInTheDocument();
      expect(screen.getByText(/Critical defensive position\s+Controls key trade routes/)).toBeInTheDocument();

      // TagArray
      expect(screen.getByText('Fort Alpha')).toBeInTheDocument();
      expect(screen.getByText('Naval Base Beta')).toBeInTheDocument();
      expect(screen.getByText('Airfield Gamma')).toBeInTheDocument();
    });
  });
});

describe('T032: DynamicPropertiesView - Fallback to Object.entries()', () => {
  it('should use generic renderer when entity type has no schema', () => {
    const properties = {
      CustomField1: 'Value 1',
      AnotherProperty: 123,
      NestedObject: { nested: 'data' },
    };

    render(
      <DynamicPropertiesView
        entityType={WorldEntityType.Folder} // No schema
        value={properties}
      />
    );

    // Should still render section header with "Custom Properties"
    expect(screen.getByText('Folder Properties')).toBeInTheDocument();

    // Generic Object.entries() keys (capitalized with spaces)
    expect(screen.getByText('Custom Field1')).toBeInTheDocument();
    expect(screen.getByText('Another Property')).toBeInTheDocument();
    expect(screen.getByText('Nested Object')).toBeInTheDocument();

    // Generic values (stringified)
    expect(screen.getByText('Value 1')).toBeInTheDocument();
    expect(screen.getByText('123')).toBeInTheDocument();
    // JSON is formatted with newlines, so use regex
    expect(screen.getByText(/\{\s+"nested":\s+"data"\s+\}/)).toBeInTheDocument();
  });
});

describe('DynamicPropertiesView - Edge Cases', () => {
  it('should render nothing when value is null', () => {
    const { container } = render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={null}
      />
    );

    expect(container.firstChild).toBeNull();
  });

  it('should render nothing when value is empty object', () => {
    const { container } = render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={{}}
      />
    );

    expect(container.firstChild).toBeNull();
  });

  it('should skip undefined fields in schema-based rendering', () => {
    const properties = {
      Climate: 'Temperate',
      // Population and Area are undefined (not in object)
      Terrain: 'Mountainous',
    };

    render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={properties}
      />
    );

    // Should render defined fields
    expect(screen.getByText('Climate')).toBeInTheDocument();
    expect(screen.getByText('Terrain')).toBeInTheDocument();

    // Should not render undefined fields
    expect(screen.queryByText('Population')).not.toBeInTheDocument();
    expect(screen.queryByText('Area (sq km)')).not.toBeInTheDocument();
  });

  it('should display "Not set" for null field values', () => {
    const properties = {
      Climate: null,
      Terrain: 'Desert',
    };

    render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={properties}
      />
    );

    expect(screen.getByText('Climate')).toBeInTheDocument();
    expect(screen.getByText('Not set')).toBeInTheDocument();
  });

  it('should preserve multiline text formatting', () => {
    const properties = {
      Terrain: 'Line 1\nLine 2\nLine 3',
    };

    render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={properties}
      />
    );

    // Should render with whitespace-pre-wrap
    const terrainValue = screen.getByText(/Line 1\s+Line 2\s+Line 3/);
    expect(terrainValue).toHaveClass('whitespace-pre-wrap');
  });
});

describe('DynamicPropertiesView - Accessibility', () => {
  it('should have no accessibility violations with schema-based rendering', async () => {
    const properties = {
      Climate: 'Tropical',
      Terrain: 'Rainforest',
      Population: 1000000,
      Area: 5000.5,
    };

    const { container } = render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={properties}
      />
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should have no accessibility violations with tagArray rendering', async () => {
    const properties = {
      Languages: ['English', 'Spanish', 'French'],
      Religions: ['Christianity', 'Buddhism'],
      CulturalTraits: 'Diverse traditions',
    };

    const { container } = render(
      <DynamicPropertiesView
        entityType={WorldEntityType.CulturalRegion}
        value={properties}
      />
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should have no accessibility violations with fallback renderer', async () => {
    const properties = {
      CustomField: 'Custom value',
      NumericField: 42,
    };

    const { container } = render(
      <DynamicPropertiesView
        entityType={WorldEntityType.Character}
        value={properties}
      />
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should use semantic HTML with proper heading hierarchy', () => {
    const properties = {
      Climate: 'Temperate',
    };

    render(
      <DynamicPropertiesView
        entityType={WorldEntityType.GeographicRegion}
        value={properties}
      />
    );

    // Section header should be h2
    const heading = screen.getByRole('heading', { level: 2, name: /Geographic Region Properties/i });
    expect(heading).toBeInTheDocument();

    // Should use definition list for key-value pairs
    const climateTerm = screen.getByText('Climate');
    const definitionList = climateTerm.closest('dl');
    expect(definitionList).toBeInTheDocument();
    expect(definitionList?.tagName).toBe('DL');
  });
});
