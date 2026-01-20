/**
 * @jest-environment jsdom
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);
import {
  MilitaryRegionProperties,
  type MilitaryRegionPropertiesData,
} from './MilitaryRegionProperties';

describe('MilitaryRegionProperties', () => {
  const defaultValue: MilitaryRegionPropertiesData = {
    CommandStructure: 'General → Colonel → Captain → Lieutenant',
    StrategicImportance: 'Controls the northern border and vital supply routes',
    MilitaryAssets: ['Fort Alpha', '3rd Legion', 'Naval Base'],
  };

  describe('Rendering', () => {
    it('should render all fields in edit mode', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Command Structure')).toBeInTheDocument();
      expect(screen.getByLabelText('Strategic Importance')).toBeInTheDocument();
      expect(screen.getByLabelText('Military Assets')).toBeInTheDocument();
    });

    it('should render title heading', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties value={{}} onChange={onChange} />
      );

      expect(
        screen.getByRole('heading', { name: 'Military Properties' })
      ).toBeInTheDocument();
    });

    it('should populate fields with provided values', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties value={defaultValue} onChange={onChange} />
      );

      expect(screen.getByLabelText('Command Structure')).toHaveValue(
        'General → Colonel → Captain → Lieutenant'
      );
      expect(screen.getByLabelText('Strategic Importance')).toHaveValue(
        'Controls the northern border and vital supply routes'
      );
      expect(screen.getByText('Fort Alpha')).toBeInTheDocument();
      expect(screen.getByText('3rd Legion')).toBeInTheDocument();
      expect(screen.getByText('Naval Base')).toBeInTheDocument();
    });

    it('should render in read-only mode with values', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={defaultValue}
          onChange={onChange}
          readOnly
        />
      );

      expect(screen.getByText('General → Colonel → Captain → Lieutenant')).toBeInTheDocument();
      expect(screen.getByText('Controls the northern border and vital supply routes')).toBeInTheDocument();
      expect(screen.getByText('Fort Alpha')).toBeInTheDocument();
      expect(screen.queryByLabelText('Command Structure')).not.toBeInTheDocument();
    });
  });

  describe('Command Structure Field', () => {
    it('should update command structure on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties value={{}} onChange={onChange} />
      );

      const commandStructure = screen.getByLabelText('Command Structure');
      await user.type(commandStructure, 'Marshal → General');

      expect(onChange).toHaveBeenLastCalledWith({
        CommandStructure: 'Marshal → General',
      });
    });

    it('should show character count for command structure', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={{ CommandStructure: 'Test hierarchy' }}
          onChange={onChange}
        />
      );

      expect(screen.getByText('14/300 characters')).toBeInTheDocument();
    });

    it('should clear command structure when emptied', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={{ CommandStructure: 'General → Colonel' }}
          onChange={onChange}
        />
      );

      const commandStructure = screen.getByLabelText('Command Structure');
      await user.clear(commandStructure);

      expect(onChange).toHaveBeenCalledWith({ CommandStructure: undefined });
    });
  });

  describe('Strategic Importance Field', () => {
    it('should update strategic importance on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties value={{}} onChange={onChange} />
      );

      const strategicImportance = screen.getByLabelText('Strategic Importance');
      await user.type(strategicImportance, 'Key defensive position');

      expect(onChange).toHaveBeenLastCalledWith({
        StrategicImportance: 'Key defensive position',
      });
    });

    it('should show character count for strategic importance', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={{ StrategicImportance: 'Important region' }}
          onChange={onChange}
        />
      );

      expect(screen.getByText('16/300 characters')).toBeInTheDocument();
    });

    it('should clear strategic importance when emptied', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={{ StrategicImportance: 'Some importance' }}
          onChange={onChange}
        />
      );

      const strategicImportance = screen.getByLabelText('Strategic Importance');
      await user.clear(strategicImportance);

      expect(onChange).toHaveBeenCalledWith({ StrategicImportance: undefined });
    });
  });

  describe('Military Assets Field', () => {
    it('should add military assets', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties value={{}} onChange={onChange} />
      );

      const input = screen.getByPlaceholderText('Add a military asset...');
      await user.type(input, 'Armored Division{Enter}');

      expect(onChange).toHaveBeenCalledWith({
        MilitaryAssets: ['Armored Division'],
      });
    });

    it('should remove military assets', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={{ MilitaryAssets: ['Fort A', 'Fort B'] }}
          onChange={onChange}
        />
      );

      const removeButtons = screen.getAllByLabelText(/remove/i);
      await user.click(removeButtons[0]);

      expect(onChange).toHaveBeenCalledWith({
        MilitaryAssets: ['Fort B'],
      });
    });

    it('should clear military assets when all removed', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={{ MilitaryAssets: ['Fort A'] }}
          onChange={onChange}
        />
      );

      const removeButton = screen.getByLabelText(/remove/i);
      await user.click(removeButton);

      expect(onChange).toHaveBeenCalledWith({
        MilitaryAssets: undefined,
      });
    });
  });

  describe('Disabled State', () => {
    it('should disable all fields when disabled prop is true', () => {
      const onChange = vi.fn();
      render(
        <MilitaryRegionProperties
          value={defaultValue}
          onChange={onChange}
          disabled
        />
      );

      expect(screen.getByLabelText('Command Structure')).toBeDisabled();
      expect(screen.getByLabelText('Strategic Importance')).toBeDisabled();
      expect(screen.getByPlaceholderText('Add a military asset...')).toBeDisabled();
    });
  });

  describe('Value Synchronization', () => {
    it('should update fields when value prop changes', () => {
      const onChange = vi.fn();
      const { rerender } = render(
        <MilitaryRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Command Structure')).toHaveValue('');
      expect(screen.queryByText('Fort Alpha')).not.toBeInTheDocument();

      rerender(
        <MilitaryRegionProperties
          value={defaultValue}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Command Structure')).toHaveValue(
        'General → Colonel → Captain → Lieutenant'
      );
      expect(screen.getByText('Fort Alpha')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it(
      'should have no accessibility violations',
      async () => {
        const onChange = vi.fn();
        const { container } = render(
          <MilitaryRegionProperties value={defaultValue} onChange={onChange} />
        );

        const results = await axe(container);
        expect(results).toHaveNoViolations();
      },
      10000
    );

    it(
      'should have no accessibility violations in read-only mode',
      async () => {
        const onChange = vi.fn();
        const { container } = render(
          <MilitaryRegionProperties
            value={defaultValue}
            onChange={onChange}
            readOnly
          />
        );

        const results = await axe(container);
        expect(results).toHaveNoViolations();
      },
      10000
    );

    it(
      'should have no accessibility violations in disabled state',
      async () => {
        const onChange = vi.fn();
        const { container } = render(
          <MilitaryRegionProperties
            value={defaultValue}
            onChange={onChange}
            disabled
          />
        );

        const results = await axe(container);
        expect(results).toHaveNoViolations();
      },
      10000
    );
  });
});
