import React from 'react';
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import MainPanel from '../components/MainPanel/MainPanel';
import SidePanel from '../components/SidePanel/SidePanel';
import ChatWindow from '../components/ChatWindow/ChatWindow';
import { ThemeProvider } from '../theme/ThemeProvider';
import { Provider } from 'react-redux';
import { store } from '../store/store';
import { expect, it, describe } from 'vitest';

expect.extend(toHaveNoViolations);

describe('a11y', () => {
  it('MainPanel has no obvious a11y violations', async () => {
    const { container } = render(
      <ThemeProvider>
        <MainPanel />
      </ThemeProvider>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('SidePanel has no obvious a11y violations', async () => {
    const { container } = render(
      <ThemeProvider>
        <Provider store={store}>
          <SidePanel />
        </Provider>
      </ThemeProvider>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('ChatWindow has no obvious a11y violations', async () => {
    const { container } = render(
      <ThemeProvider>
        <ChatWindow />
      </ThemeProvider>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
