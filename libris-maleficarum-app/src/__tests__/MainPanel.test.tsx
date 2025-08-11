import React from 'react';
import { render, screen } from '@testing-library/react';
import MainPanel from '../components/MainPanel/MainPanel';

it('renders MainPanel heading', () => {
  render(<MainPanel />);
  expect(screen.getByRole('heading', { name: /libris maleficarum/i })).toBeInTheDocument();
});
