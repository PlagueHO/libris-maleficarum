import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { CopilotKit } from '@copilotkit/react-core';
import { store } from '../../store/store';
import ChatWindow from './ChatWindow';
import { describe, it } from 'vitest';

const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <Provider store={store}>
      <CopilotKit
        runtimeUrl="https://mock-agent.example.com/api/copilotkit"
        agent="test-agent"
        publicApiKey="demo-mode"
      >
        {component}
      </CopilotKit>
    </Provider>
  );
};

// TODO: CopilotKit CSS import issue - need to configure Vitest to handle katex CSS in node_modules
// See: https://github.com/vitest-dev/vitest/issues/2834
describe.skip('ChatWindow', () => {
it('toggles ChatWindow visibility', () => {
  renderWithProviders(<ChatWindow />);
  // Initially hidden; should render collapsed rail button
  expect(screen.getByLabelText(/Show Chat Window/i)).toBeInTheDocument();
});

it('expands chat window when toggle is clicked', () => {
  renderWithProviders(<ChatWindow />);
  // Expand
  fireEvent.click(screen.getByLabelText(/Show Chat Window/i));
  // Should now show the chat interface
  expect(screen.getByRole('region', { name: /World Builder AI Assistant/i })).toBeInTheDocument();
});
});
