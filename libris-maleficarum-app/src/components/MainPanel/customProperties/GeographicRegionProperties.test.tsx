/**
 * @jest-environment jsdom
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);
import {
  GeographicRegionProperties,
  type GeographicRegionPropertiesData,
} from './GeographicRegionProperties';

describe('GeographicRegionProperties', () => {
  const defaultValue: GeographicRegionPropertiesData = {
    Climate: 'Temperate',
    Terrain: 'Mountainous',
    Population: 1000000,
    Area: 150000.5,
  };

  describe('Rendering', () => {
    it('should render all fields in edit mode', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Climate')).toBeInTheDocument();
      expect(screen.getByLabelText('Terrain')).toBeInTheDocument();
      expect(screen.getByLabelText('Population')).toBeInTheDocument();
      expect(screen.getByLabelText(/Area/i)).toBeInTheDocument();
    });

    it('should render title heading', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      expect(
        screen.getByRole('heading', { name: 'Geographic Properties' })
      ).toBeInTheDocument();
    });

    it('should populate fields with provided values', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={defaultValue} onChange={onChange} />
      );

      expect(screen.getByLabelText('Climate')).toHaveValue('Temperate');
      expect(screen.getByLabelText('Terrain')).toHaveValue('Mountainous');
      expect(screen.getByLabelText('Population')).toHaveValue('1,000,000');
      expect(screen.getByLabelText(/Area/i)).toHaveValue('150,000.50');
    });

    it('should render in read-only mode with values', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties
          value={defaultValue}
          onChange={onChange}
          readOnly
        />
      );

      expect(screen.getByText('Temperate')).toBeInTheDocument();
      expect(screen.getByText('Mountainous')).toBeInTheDocument();
      expect(screen.getByText('1,000,000')).toBeInTheDocument();
      expect(screen.getByText('150,000.50')).toBeInTheDocument();
      expect(screen.queryByLabelText('Climate')).not.toBeInTheDocument();
    });
  });

  describe('Climate Field', () => {
    it('should update climate on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const climate = screen.getByLabelText('Climate');
      await user.type(climate, 'Tropical');

      expect(onChange).toHaveBeenCalledWith({ Climate: 'T' });
      expect(onChange).toHaveBeenCalledWith({ Climate: 'Tr' });
      expect(onChange).toHaveBeenLastCalledWith({ Climate: 'Tropical' });
    });

    it('should show character count for climate', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties
          value={{ Climate: 'Temperate' }}
          onChange={onChange}
        />
      );

      expect(screen.getByText('9/200 characters')).toBeInTheDocument();
    });
  });

  describe('Terrain Field', () => {
    it('should update terrain on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const terrain = screen.getByLabelText('Terrain');
      await user.type(terrain, 'Plains');

      expect(onChange).toHaveBeenLastCalledWith({ Terrain: 'Plains' });
    });

    it('should show character count for terrain', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties
          value={{ Terrain: 'Coastal' }}
          onChange={onChange}
        />
      );

      expect(screen.getByText('7/200 characters')).toBeInTheDocument();
    });
  });

  describe('Population Field', () => {
    it('should update population with valid integer', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const population = screen.getByLabelText('Population');
      await user.type(population, '500000');

      expect(onChange).toHaveBeenLastCalledWith({ Population: 500000 });
    });

    it('should show error for invalid population (decimal)', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const population = screen.getByLabelText('Population');
      await user.type(population, '123.45');

      expect(screen.getByRole('alert')).toHaveTextContent(
        /must be a whole number/i
      );
    });

    it('should show error for negative population', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const population = screen.getByLabelText('Population');
      await user.type(population, '-100');

      expect(screen.getByRole('alert')).toHaveTextContent(
        /non-negative/i
      );
    });

    it('should format population when value changes', () => {
      const onChange = vi.fn();
      const { rerender } = render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const population = screen.getByLabelText('Population');
      expect(population).toHaveValue('');

      rerender(
        <GeographicRegionProperties
          value={{ Population: 1234567 }}
          onChange={onChange}
        />
      );

      expect(population).toHaveValue('1,234,567');
    });

    it('should have aria-invalid when population has error', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const population = screen.getByLabelText('Population');
      await user.type(population, '-50');

      expect(population).toHaveAttribute('aria-invalid', 'true');
      expect(population).toHaveAttribute('aria-describedby', 'population-error');
    });
  });

  describe('Area Field', () => {
    it('should update area with valid decimal', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const area = screen.getByLabelText(/Area/i);
      await user.type(area, '12345.67');

      expect(onChange).toHaveBeenLastCalledWith({ Area: 12345.67 });
    });

    it('should update area with integer', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const area = screen.getByLabelText(/Area/i);
      await user.type(area, '10000');

      expect(onChange).toHaveBeenLastCalledWith({ Area: 10000 });
    });

    it('should show error for negative area', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const area = screen.getByLabelText(/Area/i);
      await user.type(area, '-500');

      expect(screen.getByRole('alert')).toHaveTextContent(
        /non-negative/i
      );
    });

    it('should format area when value changes', () => {
      const onChange = vi.fn();
      const { rerender } = render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const area = screen.getByLabelText(/Area/i);
      expect(area).toHaveValue('');

      rerender(
        <GeographicRegionProperties
          value={{ Area: 123456.78 }}
          onChange={onChange}
        />
      );

      expect(area).toHaveValue('123,456.78');
    });

    it('should have aria-invalid when area has error', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      const area = screen.getByLabelText(/Area/i);
      await user.type(area, '-100.5');

      expect(area).toHaveAttribute('aria-invalid', 'true');
      expect(area).toHaveAttribute('aria-describedby', 'area-error');
    });
  });

  describe('Disabled State', () => {
    it('should disable all fields when disabled prop is true', () => {
      const onChange = vi.fn();
      render(
        <GeographicRegionProperties
          value={defaultValue}
          onChange={onChange}
          disabled
        />
      );

      expect(screen.getByLabelText('Climate')).toBeDisabled();
      expect(screen.getByLabelText('Terrain')).toBeDisabled();
      expect(screen.getByLabelText('Population')).toBeDisabled();
      expect(screen.getByLabelText(/Area/i)).toBeDisabled();
    });
  });

  describe('Value Synchronization', () => {
    it('should update fields when value prop changes', () => {
      const onChange = vi.fn();
      const { rerender } = render(
        <GeographicRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Climate')).toHaveValue('');
      expect(screen.getByLabelText('Population')).toHaveValue('');

      rerender(
        <GeographicRegionProperties
          value={defaultValue}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Climate')).toHaveValue('Temperate');
      expect(screen.getByLabelText('Population')).toHaveValue('1,000,000');
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <GeographicRegionProperties value={defaultValue} onChange={onChange} />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations in read-only mode', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <GeographicRegionProperties
          value={defaultValue}
          onChange={onChange}
          readOnly
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations in disabled state', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <GeographicRegionProperties
          value={defaultValue}
          onChange={onChange}
          disabled
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });
  });
});
