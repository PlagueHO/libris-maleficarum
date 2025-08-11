import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import ChatWindow from '../components/ChatWindow/ChatWindow';

it('toggles ChatWindow visibility', () => {
  render(<ChatWindow />);
  // Initially hidden; should render collapsed rail button
  expect(screen.getByLabelText(/Show Chat Window/i)).toBeInTheDocument();
});

it('prevents send when input is empty', () => {
  render(<ChatWindow />);
  // Expand first
  fireEvent.click(screen.getByLabelText(/Show Chat Window/i));
  const sendButton = screen.getByLabelText(/Send message/i);
  expect(sendButton).toBeDisabled();
});
