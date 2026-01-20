/**
 * Tests for TagInput component
 *
 * @module components/shared/TagInput/TagInput.test
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { TagInput } from './TagInput';

expect.extend(toHaveNoViolations);

describe('TagInput', () => {
  describe('rendering', () => {
    it('renders with label and input', () => {
      render(<TagInput label="Languages" value={[]} onChange={vi.fn()} />);
      expect(screen.getByLabelText('Languages input')).toBeInTheDocument();
      expect(screen.getByText('Languages')).toBeInTheDocument();
    });

    it('renders with placeholder', () => {
      render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={vi.fn()}
          placeholder="Add languages"
        />
      );
      expect(screen.getByPlaceholderText('Add languages')).toBeInTheDocument();
    });

    it('renders required indicator when required', () => {
      render(
        <TagInput label="Languages" value={[]} onChange={vi.fn()} required />
      );
      expect(screen.getByText('*')).toBeInTheDocument();
    });

    it('renders description when provided', () => {
      render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={vi.fn()}
          description="Enter spoken languages"
        />
      );
      expect(screen.getByText('Enter spoken languages')).toBeInTheDocument();
    });

    it('displays existing tags', () => {
      render(
        <TagInput
          label="Languages"
          value={['English', 'French', 'Spanish']}
          onChange={vi.fn()}
        />
      );
      expect(screen.getByText('English')).toBeInTheDocument();
      expect(screen.getByText('French')).toBeInTheDocument();
      expect(screen.getByText('Spanish')).toBeInTheDocument();
    });
  });

  describe('adding tags', () => {
    it('adds tag on Enter key', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(<TagInput label="Languages" value={[]} onChange={onChange} />);

      const input = screen.getByLabelText('Languages input');
      await user.type(input, 'English{Enter}');

      expect(onChange).toHaveBeenCalledWith(['English']);
    });

    it('trims whitespace from tags', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(<TagInput label="Languages" value={[]} onChange={onChange} />);

      const input = screen.getByLabelText('Languages input');
      await user.type(input, '  English  {Enter}');

      expect(onChange).toHaveBeenCalledWith(['English']);
    });

    it('clears input after adding tag', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(<TagInput label="Languages" value={[]} onChange={onChange} />);

      const input = screen.getByLabelText('Languages input') as HTMLInputElement;
      await user.type(input, 'English{Enter}');

      expect(input.value).toBe('');
    });

    it('does not add empty tag', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(<TagInput label="Languages" value={[]} onChange={onChange} />);

      const input = screen.getByLabelText('Languages input');
      await user.type(input, '   {Enter}');

      expect(onChange).not.toHaveBeenCalled();
    });

    it('prevents duplicate tags', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <TagInput label="Languages" value={['English']} onChange={onChange} />
      );

      const input = screen.getByLabelText('Languages input');
      await user.type(input, 'English{Enter}');

      expect(onChange).not.toHaveBeenCalled();
      expect(screen.getByText('Tag already exists')).toBeInTheDocument();
    });

    it('validates max length', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={onChange}
          maxLength={10}
        />
      );

      const input = screen.getByLabelText('Languages input');
      await user.type(input, 'VeryLongLanguageName{Enter}');

      expect(onChange).not.toHaveBeenCalled();
      expect(
        screen.getByText('Tag must be 10 characters or less')
      ).toBeInTheDocument();
    });

    it('clears error on input change', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <TagInput
          label="Languages"
          value={['English']}
          onChange={onChange}
          maxLength={10}
        />
      );

      const input = screen.getByLabelText('Languages input');
      
      // Trigger error
      await user.type(input, 'English{Enter}');
      expect(screen.getByText('Tag already exists')).toBeInTheDocument();

      // Error should clear on input change
      await user.type(input, 'F');
      expect(screen.queryByText('Tag already exists')).not.toBeInTheDocument();
    });
  });

  describe('removing tags', () => {
    it('removes tag on dismiss button click', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <TagInput
          label="Languages"
          value={['English', 'French']}
          onChange={onChange}
        />
      );

      const dismissButtons = screen.getAllByRole('button', {
        name: /Remove/i,
      });
      await user.click(dismissButtons[0]);

      expect(onChange).toHaveBeenCalledWith(['French']);
    });

    it('removes correct tag when multiple exist', async () => {
      const user = userEvent.setup();
      const onChange = vi.fn();

      render(
        <TagInput
          label="Languages"
          value={['English', 'French', 'Spanish']}
          onChange={onChange}
        />
      );

      const removeButton = screen.getByRole('button', {
        name: 'Remove French',
      });
      await user.click(removeButton);

      expect(onChange).toHaveBeenCalledWith(['English', 'Spanish']);
    });
  });

  describe('disabled state', () => {
    it('disables input when disabled prop is true', () => {
      render(
        <TagInput label="Languages" value={[]} onChange={vi.fn()} disabled />
      );

      const input = screen.getByLabelText('Languages input');
      expect(input).toBeDisabled();
    });

    it('disables remove buttons when disabled', () => {
      render(
        <TagInput
          label="Languages"
          value={['English']}
          onChange={vi.fn()}
          disabled
        />
      );

      const removeButton = screen.getByRole('button', {
        name: 'Remove English',
      });
      expect(removeButton).toBeDisabled();
    });
  });

  describe('error display', () => {
    it('displays external error message', () => {
      render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={vi.fn()}
          error="At least one language is required"
        />
      );

      expect(
        screen.getByText('At least one language is required')
      ).toBeInTheDocument();
    });

    it('sets aria-invalid when error exists', () => {
      render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={vi.fn()}
          error="Required field"
        />
      );

      const input = screen.getByLabelText('Languages input');
      expect(input).toHaveAttribute('aria-invalid', 'true');
    });

    it('links error to input with aria-describedby', () => {
      render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={vi.fn()}
          error="Required field"
        />
      );

      const input = screen.getByLabelText('Languages input');
      const errorId = input.getAttribute('aria-describedby');
      expect(errorId).toBeTruthy();

      const errorElement = document.getElementById(errorId!);
      expect(errorElement).toHaveTextContent('Required field');
    });
  });

  describe('accessibility', () => {
    it('has no accessibility violations with empty tags', async () => {
      const { container } = render(
        <TagInput label="Languages" value={[]} onChange={vi.fn()} />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('has no accessibility violations with tags', async () => {
      const { container } = render(
        <TagInput
          label="Languages"
          value={['English', 'French']}
          onChange={vi.fn()}
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('has no accessibility violations with error', async () => {
      const { container } = render(
        <TagInput
          label="Languages"
          value={[]}
          onChange={vi.fn()}
          error="Required field"
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('has no accessibility violations when disabled', async () => {
      const { container } = render(
        <TagInput
          label="Languages"
          value={['English']}
          onChange={vi.fn()}
          disabled
        />
      );

      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('tags are in accessible list', () => {
      render(
        <TagInput
          label="Languages"
          value={['English', 'French']}
          onChange={vi.fn()}
        />
      );

      const list = screen.getByRole('list', { name: 'Languages tags' });
      expect(list).toBeInTheDocument();

      const items = screen.getAllByRole('listitem');
      expect(items).toHaveLength(2);
    });
  });
});
