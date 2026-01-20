/**
 * @jest-environment jsdom
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);
import {
  CulturalRegionProperties,
  type CulturalRegionPropertiesData,
} from './CulturalRegionProperties';

describe('CulturalRegionProperties', () => {
  const defaultValue: CulturalRegionPropertiesData = {
    Languages: ['Common', 'Elvish', 'Dwarvish'],
    Religions: ['The Old Faith', 'Sun Worship'],
    CulturalTraits: 'Emphasis on craftsmanship and oral traditions',
  };

  describe('Rendering', () => {
    it('should render all fields in edit mode', () => {
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Languages')).toBeInTheDocument();
      expect(screen.getByLabelText('Religions')).toBeInTheDocument();
      expect(screen.getByLabelText('Cultural Traits')).toBeInTheDocument();
    });

    it('should render title heading', () => {
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties value={{}} onChange={onChange} />
      );

      expect(
        screen.getByRole('heading', { name: 'Cultural Properties' })
      ).toBeInTheDocument();
    });

    it('should populate fields with provided values', () => {
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties value={defaultValue} onChange={onChange} />
      );

      expect(screen.getByText('Common')).toBeInTheDocument();
      expect(screen.getByText('Elvish')).toBeInTheDocument();
      expect(screen.getByText('Dwarvish')).toBeInTheDocument();
      expect(screen.getByText('The Old Faith')).toBeInTheDocument();
      expect(screen.getByText('Sun Worship')).toBeInTheDocument();
      expect(screen.getByLabelText('Cultural Traits')).toHaveValue(
        'Emphasis on craftsmanship and oral traditions'
      );
    });

    it('should render in read-only mode with values', () => {
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={defaultValue}
          onChange={onChange}
          readOnly
        />
      );

      expect(screen.getByText('Common')).toBeInTheDocument();
      expect(screen.getByText('The Old Faith')).toBeInTheDocument();
      expect(screen.getByText('Emphasis on craftsmanship and oral traditions')).toBeInTheDocument();
      expect(screen.queryByLabelText('Languages')).not.toBeInTheDocument();
    });
  });

  describe('Languages Field', () => {
    it('should add languages', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties value={{}} onChange={onChange} />
      );

      const input = screen.getByPlaceholderText('Add a language...');
      await user.type(input, 'Orcish{Enter}');

      expect(onChange).toHaveBeenCalledWith({
        Languages: ['Orcish'],
      });
    });

    it('should remove languages', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={{ Languages: ['Common', 'Elvish'] }}
          onChange={onChange}
        />
      );

      const removeButtons = screen.getAllByLabelText(/remove/i);
      await user.click(removeButtons[0]);

      expect(onChange).toHaveBeenCalledWith({
        Languages: ['Elvish'],
      });
    });

    it('should clear languages when all removed', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={{ Languages: ['Common'] }}
          onChange={onChange}
        />
      );

      const removeButton = screen.getByLabelText(/remove/i);
      await user.click(removeButton);

      expect(onChange).toHaveBeenCalledWith({
        Languages: undefined,
      });
    });
  });

  describe('Religions Field', () => {
    it('should add religions', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties value={{}} onChange={onChange} />
      );

      const inputs = screen.getAllByRole('textbox');
      const religionInput = inputs.find((input) =>
        input.getAttribute('placeholder')?.includes('religion')
      );

      if (religionInput) {
        await user.type(religionInput, 'Moon Cult{Enter}');
      }

      expect(onChange).toHaveBeenCalledWith({
        Religions: ['Moon Cult'],
      });
    });

    it('should remove religions', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={{ Religions: ['Faith A', 'Faith B'] }}
          onChange={onChange}
        />
      );

      const removeButtons = screen.getAllByLabelText(/remove/i);
      await user.click(removeButtons[0]);

      expect(onChange).toHaveBeenCalledWith({
        Religions: ['Faith B'],
      });
    });

    it('should clear religions when all removed', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={{ Religions: ['Faith A'] }}
          onChange={onChange}
        />
      );

      const removeButton = screen.getByLabelText(/remove/i);
      await user.click(removeButton);

      expect(onChange).toHaveBeenCalledWith({
        Religions: undefined,
      });
    });
  });

  describe('Cultural Traits Field', () => {
    it('should update cultural traits on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties value={{}} onChange={onChange} />
      );

      const culturalTraits = screen.getByLabelText('Cultural Traits');
      await user.type(culturalTraits, 'Strong warrior culture');

      expect(onChange).toHaveBeenLastCalledWith({
        CulturalTraits: 'Strong warrior culture',
      });
    });

    it('should show character count for cultural traits', () => {
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={{ CulturalTraits: 'Test traits' }}
          onChange={onChange}
        />
      );

      expect(screen.getByText('11/500 characters')).toBeInTheDocument();
    });

    it('should clear cultural traits when emptied', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={{ CulturalTraits: 'Some traits' }}
          onChange={onChange}
        />
      );

      const culturalTraits = screen.getByLabelText('Cultural Traits');
      await user.clear(culturalTraits);

      expect(onChange).toHaveBeenCalledWith({ CulturalTraits: undefined });
    });
  });

  describe('Disabled State', () => {
    it('should disable all fields when disabled prop is true', () => {
      const onChange = vi.fn();
      render(
        <CulturalRegionProperties
          value={defaultValue}
          onChange={onChange}
          disabled
        />
      );

      expect(screen.getByPlaceholderText('Add a language...')).toBeDisabled();
      expect(screen.getByLabelText('Cultural Traits')).toBeDisabled();
    });
  });

  describe('Value Synchronization', () => {
    it('should update fields when value prop changes', () => {
      const onChange = vi.fn();
      const { rerender } = render(
        <CulturalRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.queryByText('Common')).not.toBeInTheDocument();
      expect(screen.getByLabelText('Cultural Traits')).toHaveValue('');

      rerender(
        <CulturalRegionProperties
          value={defaultValue}
          onChange={onChange}
        />
      );

      expect(screen.getByText('Common')).toBeInTheDocument();
      expect(screen.getByLabelText('Cultural Traits')).toHaveValue(
        'Emphasis on craftsmanship and oral traditions'
      );
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <CulturalRegionProperties value={defaultValue} onChange={onChange} />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations in read-only mode', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <CulturalRegionProperties
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
        <CulturalRegionProperties
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
