import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { Provider } from "react-redux";
import { CopilotKit } from "@copilotkit/react-core";
import { store } from "../../store/store";
import { WorldBuilderChat } from "./WorldBuilderChat";

// TODO: CopilotKit CSS import issue - need to configure Vitest to handle katex CSS in node_modules
// See: https://github.com/vitest-dev/vitest/issues/2834
describe.skip("WorldBuilderChat", () => {
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

  it("should render the chat component", async () => {
    const { container } = renderWithProviders(<WorldBuilderChat />);
    // CopilotKit renders the chat container - wait for it to appear
    await waitFor(
      () => {
        expect(container.querySelector(".copilotKitChat")).toBeInTheDocument();
      },
      { timeout: 3000 }
    );
  });

  it("should display the initial welcome message", async () => {
    renderWithProviders(<WorldBuilderChat />);
    // CopilotKit displays the initial message - wait for it to appear
    await waitFor(
      () => {
        expect(
          screen.getByText(/Welcome to Libris Maleficarum/i)
        ).toBeInTheDocument();
      },
      { timeout: 3000 }
    );
  });

  it("should have a text input for messages", async () => {
    renderWithProviders(<WorldBuilderChat />);
    // CopilotKit provides a message input - wait for it to appear
    await waitFor(
      () => {
        expect(
          screen.getByPlaceholderText(/Type a message/i)
        ).toBeInTheDocument();
      },
      { timeout: 3000 }
    );
  });
});
