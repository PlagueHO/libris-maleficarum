/// <reference types="vitest/globals" />
/// <reference types="@testing-library/jest-dom" />

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import App from './App';

// Create a mock store for testing
const createMockStore = () => {
  return configureStore({
    reducer: {
      sidePanel: (state = { isExpanded: true }) => state,
    },
  });
};

describe('App', () => {
  it('renders without crashing', () => {
    const store = createMockStore();
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );
    expect(screen.getByRole('heading', { name: /Libris Maleficarum/i })).toBeInTheDocument();
  });
});
