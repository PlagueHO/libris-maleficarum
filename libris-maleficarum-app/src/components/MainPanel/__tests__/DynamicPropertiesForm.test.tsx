/**
 * Unit tests for DynamicPropertiesForm component
 *
 * Tests schema-driven form rendering for all Regional entity types,
 * field aggregation logic, and onChange callbacks.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { DynamicPropertiesForm } from '../DynamicPropertiesForm';

expect.extend(toHaveNoViolations);

describe('DynamicPropertiesForm', () => {
  describe('T027: Schema-driven form rendering', () => {
    it('should render section header with entity type label', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={{}}
          onChange={onChange}
        />
      );

      expect(screen.getByText('Geographic Region Properties')).toBeInTheDocument();
    });

    it('should render all schema fields for GeographicRegion', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={{}}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Climate')).toBeInTheDocument();
      expect(screen.getByLabelText('Terrain')).toBeInTheDocument();
      expect(screen.getByLabelText('Population')).toBeInTheDocument();
      expect(screen.getByLabelText('Area (sq km)')).toBeInTheDocument();
    });

    it('should render all schema fields for PoliticalRegion', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="PoliticalRegion"
          value={{}}
          onChange={onChange}
        />
      );

      expect(screen.getByText('Political Region Properties')).toBeInTheDocument();
      expect(screen.getByLabelText('Government Type')).toBeInTheDocument();
      expect(screen.getByLabelText('Member States')).toBeInTheDocument();
      expect(screen.getByLabelText('Established Date')).toBeInTheDocument();
    });

    it('should render all schema fields for CulturalRegion', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="CulturalRegion"
          value={{}}
          onChange={onChange}
        />
      );

      expect(screen.getByText('Cultural Region Properties')).toBeInTheDocument();
      expect(screen.getByLabelText('Languages')).toBeInTheDocument();
      expect(screen.getByLabelText('Religions')).toBeInTheDocument();
      expect(screen.getByLabelText('Cultural Traits')).toBeInTheDocument();
    });

    it('should render all schema fields for MilitaryRegion', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="MilitaryRegion"
          value={{}}
          onChange={onChange}
        />
      );

      expect(screen.getByText('Military Region Properties')).toBeInTheDocument();
      expect(screen.getByLabelText('Command Structure')).toBeInTheDocument();
      expect(screen.getByLabelText('Strategic Importance')).toBeInTheDocument();
      expect(screen.getByLabelText('Military Assets')).toBeInTheDocument();
    });

    it('should not render anything for entity types without schema', () => {
      const onChange = vi.fn();
      const { container } = render(
        <DynamicPropertiesForm
          entityType="Character"
          value={{}}
          onChange={onChange}
        />
      );

      expect(container.firstChild).toBeNull();
    });
  });

  describe('Field value handling', () => {
    it('should populate fields with existing property values', () => {
      const onChange = vi.fn();
      const existingProperties = {
        Climate: 'Temperate',
        Terrain: 'Mountainous',
        Population: 1000000,
        Area: 150000.5,
      };

      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={existingProperties}
          onChange={onChange}
        />
      );
      
      // Textareas for Climate and Terrain
      expect(screen.getByLabelText('Climate')).toHaveValue('Temperate');
      expect(screen.getByLabelText('Terrain')).toHaveValue('Mountainous');
      // Numeric inputs for Population and Area
      expect(screen.getByLabelText('Population')).toHaveValue('1,000,000');
      expect(screen.getByLabelText('Area (sq km)')).toHaveValue('150,000.50');
    });

    it('should handle null value gracefully', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={null}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Climate')).toHaveValue('');
      expect(screen.getByLabelText('Terrain')).toHaveValue('');
    });

    it('should handle undefined field values gracefully', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="PoliticalRegion"
          value={{ GovernmentType: undefined }}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Government Type')).toHaveValue('');
    });
  });

  describe('T021: Field aggregation and onChange', () => {
    it('should call onChange with aggregated field values when a field changes', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      const existingProperties = {
        Climate: 'Temperate',
        Terrain: 'Mountainous',
      };

      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={existingProperties}
          onChange={onChange}
        />
      );

      const climateInput = screen.getByLabelText('Climate');
      await user.clear(climateInput);
      await user.type(climateInput, 'Tropical');

      // Last call should have both existing (Terrain) and new (Climate) values
      // Note: undefined values are filtered out
      expect(onChange).toHaveBeenLastCalledWith({
        Climate: 'Tropical',
        Terrain: 'Mountainous',
      });
    });

    it('should aggregate updates from multiple fields', async () => {
      const user = userEvent.setup();
      let currentValue = {};
      const onChange = vi.fn((newValue) => {
        currentValue = newValue;
      });

      const { rerender } = render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={currentValue}
          onChange={onChange}
        />
      );

      // Type into Climate field
      const climateInput = screen.getByLabelText('Climate');
      await user.type(climateInput, 'Arid');

      // Re-render with updated value (simulating parent component behavior)
      rerender(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={currentValue}
          onChange={onChange}
        />
      );

      // Type into Terrain field
      const terrainInput = screen.getByLabelText('Terrain');
      await user.type(terrainInput, 'Desert');

      // Last call should include both fields after re-rendering with updated value
      expect(onChange).toHaveBeenLastCalledWith({
        Climate: 'Arid',
        Terrain: 'Desert',
      });
    });

    it('should filter out undefined values when calling onChange', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <DynamicPropertiesForm
          entityType="PoliticalRegion"
          value={{ GovernmentType: 'Democracy' }}
          onChange={onChange}
        />
      );

      const govInput = screen.getByLabelText('Government Type');
      await user.clear(govInput);
      await user.type(govInput, 'Republic');

      // Should only include defined values (GovernmentType, not MemberStates/EstablishedDate which are undefined)
      const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1][0];
      expect(lastCall).toHaveProperty('GovernmentType', 'Republic');
      expect(lastCall).not.toHaveProperty('MemberStates');
      expect(lastCall).not.toHaveProperty('EstablishedDate');
    });

    it('should include numeric zero values (not filter as falsy)', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={{}}
          onChange={onChange}
        />
      );

      const populationInput = screen.getByLabelText('Population');
      await user.type(populationInput, '0');

      const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1][0];
      expect(lastCall).toHaveProperty('Population', 0);
    });

    it('should include empty arrays for tagArray fields', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <DynamicPropertiesForm
          entityType="CulturalRegion"
          value={{ Languages: [] }}
          onChange={onChange}
        />
      );

      const langInput = screen.getByLabelText('Languages input');
      await user.type(langInput, 'English{Enter}');

      const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1][0];
      expect(lastCall).toHaveProperty('Languages');
      expect(Array.isArray(lastCall.Languages)).toBe(true);
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations for GeographicRegion form', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={{}}
          onChange={onChange}
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations for PoliticalRegion form', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <DynamicPropertiesForm
          entityType="PoliticalRegion"
          value={{}}
          onChange={onChange}
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should properly associate all fields with labels', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="CulturalRegion"
          value={{}}
          onChange={onChange}
        />
      );

      const langInput = screen.getByLabelText('Languages');
      const relInput = screen.getByLabelText('Religions');
      const traitsInput = screen.getByLabelText('Cultural Traits');

      expect(langInput).toBeInTheDocument();
      expect(relInput).toBeInTheDocument();
      expect(traitsInput).toBeInTheDocument();
    });
  });

  describe('Disabled state', () => {
    it('should disable all fields when disabled prop is true', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertiesForm
          entityType="GeographicRegion"
          value={{}}
          onChange={onChange}
          disabled={true}
        />
      );

      expect(screen.getByLabelText('Climate')).toBeDisabled();
      expect(screen.getByLabelText('Terrain')).toBeDisabled();
      expect(screen.getByLabelText('Population')).toBeDisabled();
      expect(screen.getByLabelText('Area (sq km)')).toBeDisabled();
    });
  });
});
