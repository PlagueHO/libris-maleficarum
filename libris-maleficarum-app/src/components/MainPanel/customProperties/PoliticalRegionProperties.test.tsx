/**
 * @jest-environment jsdom
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);
import {
  PoliticalRegionProperties,
  type PoliticalRegionPropertiesData,
} from './PoliticalRegionProperties';

describe('PoliticalRegionProperties', () => {
  const defaultValue: PoliticalRegionPropertiesData = {
    GovernmentType: 'Democracy',
    MemberStates: ['State A', 'State B', 'State C'],
    EstablishedDate: 'Year 1456',
  };

  describe('Rendering', () => {
    it('should render all fields in edit mode', () => {
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Government Type')).toBeInTheDocument();
      expect(screen.getByLabelText('Member States')).toBeInTheDocument();
      expect(screen.getByLabelText('Established Date')).toBeInTheDocument();
    });

    it('should render title heading', () => {
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      expect(
        screen.getByRole('heading', { name: 'Political Properties' })
      ).toBeInTheDocument();
    });

    it('should populate fields with provided values', () => {
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={defaultValue} onChange={onChange} />
      );

      expect(screen.getByLabelText('Government Type')).toHaveValue('Democracy');
      expect(screen.getByText('State A')).toBeInTheDocument();
      expect(screen.getByText('State B')).toBeInTheDocument();
      expect(screen.getByText('State C')).toBeInTheDocument();
      expect(screen.getByLabelText('Established Date')).toHaveValue('Year 1456');
    });

    it('should render in read-only mode with values', () => {
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={defaultValue}
          onChange={onChange}
          readOnly
        />
      );

      expect(screen.getByText('Democracy')).toBeInTheDocument();
      expect(screen.getByText('State A')).toBeInTheDocument();
      expect(screen.getByText('Year 1456')).toBeInTheDocument();
      expect(screen.queryByLabelText('Government Type')).not.toBeInTheDocument();
    });
  });

  describe('Government Type Field', () => {
    it('should update government type on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      const governmentType = screen.getByLabelText('Government Type');
      await user.type(governmentType, 'Monarchy');

      expect(onChange).toHaveBeenCalledWith({ GovernmentType: 'M' });
      expect(onChange).toHaveBeenLastCalledWith({ GovernmentType: 'Monarchy' });
    });

    it('should show character count for government type', () => {
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={{ GovernmentType: 'Democracy' }}
          onChange={onChange}
        />
      );

      expect(screen.getByText('9/200 characters')).toBeInTheDocument();
    });

    it('should clear government type when emptied', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={{ GovernmentType: 'Democracy' }}
          onChange={onChange}
        />
      );

      const governmentType = screen.getByLabelText('Government Type');
      await user.clear(governmentType);

      expect(onChange).toHaveBeenCalledWith({ GovernmentType: undefined });
    });
  });

  describe('Member States Field', () => {
    it('should add member states', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      const input = screen.getByPlaceholderText('Add a member state...');
      await user.type(input, 'New State{Enter}');

      expect(onChange).toHaveBeenCalledWith({
        MemberStates: ['New State'],
      });
    });

    it('should remove member states', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={{ MemberStates: ['State A', 'State B'] }}
          onChange={onChange}
        />
      );

      const removeButtons = screen.getAllByLabelText(/remove/i);
      await user.click(removeButtons[0]);

      expect(onChange).toHaveBeenCalledWith({
        MemberStates: ['State B'],
      });
    });

    it('should clear member states when all removed', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={{ MemberStates: ['State A'] }}
          onChange={onChange}
        />
      );

      const removeButton = screen.getByLabelText(/remove/i);
      await user.click(removeButton);

      expect(onChange).toHaveBeenCalledWith({
        MemberStates: undefined,
      });
    });
  });

  describe('Established Date Field', () => {
    it('should update established date on change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      const establishedDate = screen.getByLabelText('Established Date');
      await user.type(establishedDate, '3rd Age, Spring');

      expect(onChange).toHaveBeenLastCalledWith({
        EstablishedDate: '3rd Age, Spring',
      });
    });

    it('should clear established date when emptied', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={{ EstablishedDate: 'Year 1456' }}
          onChange={onChange}
        />
      );

      const establishedDate = screen.getByLabelText('Established Date');
      await user.clear(establishedDate);

      expect(onChange).toHaveBeenCalledWith({ EstablishedDate: undefined });
    });

    it('should support free-form date strings', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      const establishedDate = screen.getByLabelText('Established Date');
      await user.type(establishedDate, 'The Age of Dragons');

      expect(onChange).toHaveBeenLastCalledWith({
        EstablishedDate: 'The Age of Dragons',
      });
    });
  });

  describe('Disabled State', () => {
    it('should disable all fields when disabled prop is true', () => {
      const onChange = vi.fn();
      render(
        <PoliticalRegionProperties
          value={defaultValue}
          onChange={onChange}
          disabled
        />
      );

      expect(screen.getByLabelText('Government Type')).toBeDisabled();
      expect(screen.getByPlaceholderText('Add a member state...')).toBeDisabled();
      expect(screen.getByLabelText('Established Date')).toBeDisabled();
    });
  });

  describe('Value Synchronization', () => {
    it('should update fields when value prop changes', () => {
      const onChange = vi.fn();
      const { rerender } = render(
        <PoliticalRegionProperties value={{}} onChange={onChange} />
      );

      expect(screen.getByLabelText('Government Type')).toHaveValue('');
      expect(screen.queryByText('State A')).not.toBeInTheDocument();

      rerender(
        <PoliticalRegionProperties
          value={defaultValue}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Government Type')).toHaveValue('Democracy');
      expect(screen.getByText('State A')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <PoliticalRegionProperties value={defaultValue} onChange={onChange} />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations in read-only mode', async () => {
      const onChange = vi.fn();
      const { container } = render(
        <PoliticalRegionProperties
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
        <PoliticalRegionProperties
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
