/**
 * Unit tests for DynamicPropertyField component
 *
 * Tests all 5 field types (text, textarea, integer, decimal, tagArray)
 * in both edit and read-only modes with validation and accessibility coverage.
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { DynamicPropertyField } from '../DynamicPropertyField';
import type { PropertyFieldSchema } from '@/services/config/entityTypeRegistry';

expect.extend(toHaveNoViolations);

describe('DynamicPropertyField', () => {
  describe('T012: Text field type rendering', () => {
    const textSchema: PropertyFieldSchema = {
      key: 'EstablishedDate',
      label: 'Established Date',
      type: 'text',
      placeholder: 'e.g., Year 1456',
      description: 'Free-form date',
      maxLength: 100,
    };

    it('should render text input with label and placeholder', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={textSchema}
          value=""
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Established Date')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('e.g., Year 1456')).toBeInTheDocument();
      expect(screen.getByText('Free-form date')).toBeInTheDocument();
    });

    it('should display character counter when maxLength is set', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={textSchema}
          value="Year 1456"
          onChange={onChange}
        />
      );

      expect(screen.getByText('9/100 characters')).toBeInTheDocument();
    });

    it('should call onChange when text value changes', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={textSchema}
          value=""
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Established Date');
      await user.type(input, 'Year 1500');

      expect(onChange).toHaveBeenCalledWith('Year 1500');
    });

    it('should render read-only view for text field', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={textSchema}
          value="Year 1456"
          onChange={onChange}
          readOnly={true}
        />
      );

      expect(screen.getByText('Established Date')).toBeInTheDocument();
      expect(screen.getByText('Year 1456')).toBeInTheDocument();
      expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    });

    it('should show required asterisk when field is required', () => {
      const requiredSchema: PropertyFieldSchema = {
        ...textSchema,
        validation: { required: true },
      };
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={requiredSchema}
          value=""
          onChange={onChange}
        />
      );

      expect(screen.getByText('*')).toBeInTheDocument();
    });
  });

  describe('T013: Textarea field type rendering', () => {
    const textareaSchema: PropertyFieldSchema = {
      key: 'Climate',
      label: 'Climate',
      type: 'textarea',
      placeholder: 'Describe the climate...',
      maxLength: 200,
    };

    it('should render textarea with label and placeholder', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={textareaSchema}
          value=""
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Climate')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Describe the climate...')).toBeInTheDocument();
    });

    it('should display character counter', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={textareaSchema}
          value="Temperate"
          onChange={onChange}
        />
      );

      expect(screen.getByText('9/200 characters')).toBeInTheDocument();
    });

    it('should render read-only view with preserved whitespace', () => {
      const onChange = vi.fn();
      const multilineText = 'Line 1\nLine 2\nLine 3';
      render(
        <DynamicPropertyField
          schema={textareaSchema}
          value={multilineText}
          onChange={onChange}
          readOnly={true}
        />
      );

      expect(screen.getByText('Climate')).toBeInTheDocument();
      // Use regex to match text with newlines since it's preserved in whitespace-pre-wrap
      expect(screen.getByText(/Line 1\s+Line 2\s+Line 3/)).toBeInTheDocument();
    });
  });

  describe('T014: Integer field type rendering', () => {
    const integerSchema: PropertyFieldSchema = {
      key: 'Population',
      label: 'Population',
      type: 'integer',
      placeholder: 'e.g., 1,000,000',
      description: 'Whole number only',
    };

    it('should render numeric input with appropriate attributes', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={integerSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Population') as HTMLInputElement;
      expect(input).toBeInTheDocument();
      expect(input.type).toBe('text');
      expect(input.inputMode).toBe('numeric');
      expect(screen.getByText('Whole number only')).toBeInTheDocument();
    });

    it('should format integer value on blur', async () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={integerSchema}
          value={1000000}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Population');
      expect(input).toHaveValue('1,000,000');
    });

    it('should validate and coerce string to integer', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={integerSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Population');
      await user.type(input, '1000');

      // Validation happens on change - should call onChange with coerced number
      expect(onChange).toHaveBeenCalledWith(1000);
    });

    it('should show validation error for non-integer input', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={integerSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Population');
      await user.type(input, 'abc');

      expect(await screen.findByRole('alert')).toHaveTextContent('Must be a valid number');
    });

    it('should render read-only view with formatted number', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={integerSchema}
          value={1500000}
          onChange={onChange}
          readOnly={true}
        />
      );

      expect(screen.getByText('Population')).toBeInTheDocument();
      expect(screen.getByText('1,500,000')).toBeInTheDocument();
    });
  });

  describe('T015: Decimal field type rendering', () => {
    const decimalSchema: PropertyFieldSchema = {
      key: 'Area',
      label: 'Area (sq km)',
      type: 'decimal',
      placeholder: 'e.g., 150,000.50',
      description: 'Decimal values allowed',
    };

    it('should render numeric input with decimal inputMode', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={decimalSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Area (sq km)') as HTMLInputElement;
      expect(input).toBeInTheDocument();
      expect(input.inputMode).toBe('decimal');
    });

    it('should format decimal value with 2 decimal places on blur', async () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={decimalSchema}
          value={150000.5}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Area (sq km)');
      expect(input).toHaveValue('150,000.50');
    });

    it('should validate and coerce string to decimal', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={decimalSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Area (sq km)');
      await user.type(input, '123.45');

      expect(onChange).toHaveBeenCalledWith(123.45);
    });

    it('should render read-only view with formatted decimal', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={decimalSchema}
          value={150000.5}
          onChange={onChange}
          readOnly={true}
        />
      );

      expect(screen.getByText('Area (sq km)')).toBeInTheDocument();
      expect(screen.getByText('150,000.50')).toBeInTheDocument();
    });
  });

  describe('T016: TagArray field type rendering', () => {
    const tagArraySchema: PropertyFieldSchema = {
      key: 'Languages',
      label: 'Languages',
      type: 'tagArray',
      placeholder: 'Add a language...',
      description: 'Spoken languages',
      maxLength: 50,
    };

    it('should render TagInput component', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={tagArraySchema}
          value={[]}
          onChange={onChange}
        />
      );

      expect(screen.getByLabelText('Languages')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Add a language...')).toBeInTheDocument();
      expect(screen.getByText('Spoken languages')).toBeInTheDocument();
    });

    it('should display existing tags', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={tagArraySchema}
          value={['English', 'Spanish']}
          onChange={onChange}
        />
      );

      expect(screen.getByText('English')).toBeInTheDocument();
      expect(screen.getByText('Spanish')).toBeInTheDocument();
    });

    it('should call onChange with updated tags', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={tagArraySchema}
          value={['English']}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Languages input');
      await user.type(input, 'French{Enter}');

      expect(onChange).toHaveBeenCalledWith(['English', 'French']);
    });

    it('should render read-only view as badges', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={tagArraySchema}
          value={['English', 'Spanish', 'French']}
          onChange={onChange}
          readOnly={true}
        />
      );

      expect(screen.getByText('Languages')).toBeInTheDocument();
      expect(screen.getByText('English')).toBeInTheDocument();
      expect(screen.getByText('Spanish')).toBeInTheDocument();
      expect(screen.getByText('French')).toBeInTheDocument();
    });

    it('should show "-" when no tags in read-only mode', () => {
      const onChange = vi.fn();
      render(
        <DynamicPropertyField
          schema={tagArraySchema}
          value={[]}
          onChange={onChange}
          readOnly={true}
        />
      );

      expect(screen.getByText('-')).toBeInTheDocument();
    });
  });

  describe('T019: Accessibility tests', () => {
    it('should have no accessibility violations for text field', async () => {
      const onChange = vi.fn();
      const textSchema: PropertyFieldSchema = {
        key: 'name',
        label: 'Name',
        type: 'text',
      };

      const { container } = render(
        <DynamicPropertyField
          schema={textSchema}
          value=""
          onChange={onChange}
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations for integer field with error', async () => {
      const onChange = vi.fn();
      const intSchema: PropertyFieldSchema = {
        key: 'population',
        label: 'Population',
        type: 'integer',
        validation: { required: true },
      };

      const { container } = render(
        <DynamicPropertyField
          schema={intSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      // Trigger validation error
      const input = screen.getByRole('textbox', { name: /Population/i });
      const user = userEvent.setup();
      await user.type(input, 'abc');

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations for tagArray field', async () => {
      const onChange = vi.fn();
      const tagSchema: PropertyFieldSchema = {
        key: 'tags',
        label: 'Tags',
        type: 'tagArray',
      };

      const { container } = render(
        <DynamicPropertyField
          schema={tagSchema}
          value={['Tag1', 'Tag2']}
          onChange={onChange}
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should properly associate error messages with inputs using aria-describedby', async () => {
      const onChange = vi.fn();
      const intSchema: PropertyFieldSchema = {
        key: 'age',
        label: 'Age',
        type: 'integer',
      };

      render(
        <DynamicPropertyField
          schema={intSchema}
          value={undefined}
          onChange={onChange}
        />
      );

      const input = screen.getByLabelText('Age') as HTMLInputElement;
      const user = userEvent.setup();
      await user.type(input, 'invalid');

      // Wait for error to appear
      const errorMessage = await screen.findByRole('alert');
      expect(errorMessage).toBeInTheDocument();
      expect(input.getAttribute('aria-describedby')).toBe('age-error');
      expect(input.getAttribute('aria-invalid')).toBe('true');
    });
  });

  describe('Disabled state', () => {
    it('should disable input when disabled prop is true', () => {
      const onChange = vi.fn();
      const textSchema: PropertyFieldSchema = {
        key: 'name',
        label: 'Name',
        type: 'text',
      };

      render(
        <DynamicPropertyField
          schema={textSchema}
          value=""
          onChange={onChange}
          disabled={true}
        />
      );

      const input = screen.getByLabelText('Name');
      expect(input).toBeDisabled();
    });
  });
});
