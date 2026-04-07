/// <reference types="vitest/globals" />
/// <reference types="@testing-library/jest-dom" />

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { AccessCodeDialog } from './AccessCodeDialog';

expect.extend(toHaveNoViolations);

describe('AccessCodeDialog', () => {
  const defaultProps = {
    open: true,
    onSubmit: vi.fn(),
    isLoading: false,
    error: null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the title, input, and submit button', () => {
    render(<AccessCodeDialog {...defaultProps} />);

    expect(screen.getByText('Access Code Required')).toBeInTheDocument();
    expect(screen.getByLabelText('Access code')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit' })).toBeInTheDocument();
  });

  it('calls onSubmit when submit button is clicked', async () => {
    const onSubmit = vi.fn();
    render(<AccessCodeDialog {...defaultProps} onSubmit={onSubmit} />);

    const input = screen.getByLabelText('Access code');
    await userEvent.type(input, 'test-code');
    await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

    expect(onSubmit).toHaveBeenCalledWith('test-code');
  });

  it('calls onSubmit when Enter is pressed', async () => {
    const onSubmit = vi.fn();
    render(<AccessCodeDialog {...defaultProps} onSubmit={onSubmit} />);

    const input = screen.getByLabelText('Access code');
    await userEvent.type(input, 'test-code');
    fireEvent.keyDown(input, { key: 'Enter' });

    expect(onSubmit).toHaveBeenCalledWith('test-code');
  });

  it('shows loading state', () => {
    render(<AccessCodeDialog {...defaultProps} isLoading={true} />);

    expect(screen.getByText('Verifying...')).toBeInTheDocument();
    expect(screen.getByLabelText('Access code')).toBeDisabled();
  });

  it('displays error message', () => {
    render(<AccessCodeDialog {...defaultProps} error="Invalid code" />);

    expect(screen.getByRole('alert')).toHaveTextContent('Invalid code');
  });

  it('disables submit button when input is empty', () => {
    render(<AccessCodeDialog {...defaultProps} />);

    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
  });

  it('does not render when closed', () => {
    render(<AccessCodeDialog {...defaultProps} open={false} />);

    expect(screen.queryByText('Access Code Required')).not.toBeInTheDocument();
  });

  it('passes jest-axe accessibility checks', async () => {
    const { container } = render(<AccessCodeDialog {...defaultProps} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
