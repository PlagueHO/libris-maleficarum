import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { Provider } from 'react-redux';
import { CopilotKit } from '@copilotkit/react-core';
import { store } from '../../store/store';
import { WorldBuilderChat } from './WorldBuilderChat';

describe('WorldBuilderChat', () => {
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

  it('should render the chat component', () => {
    const { container } = renderWithProviders(<WorldBuilderChat />);
    // CopilotKit renders the chat container
    expect(container.querySelector('.copilotKitChat')).toBeInTheDocument();
  });

  it('should display the initial welcome message', () => {
    renderWithProviders(<WorldBuilderChat />);
    // CopilotKit displays the initial message
    expect(screen.getByText(/Welcome to Libris Maleficarum/i)).toBeInTheDocument();
  });

  it('should have a text input for messages', () => {
    renderWithProviders(<WorldBuilderChat />);
    // CopilotKit provides a message input
    expect(screen.getByPlaceholderText(/Type a message/i)).toBeInTheDocument();
  });
});
