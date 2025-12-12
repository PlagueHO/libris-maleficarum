import React from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, it, expect, vi } from "vitest";
import { ThemeProvider } from "../../theme/ThemeProvider";
import ChatWindow from "./ChatWindow";

expect.extend(toHaveNoViolations);

// Mock WorldBuilderChat to avoid CopilotKit CSS import issues
vi.mock("../WorldBuilderChat/WorldBuilderChat", () => ({
  WorldBuilderChat: () => (
    <div data-testid="world-builder-chat">World Builder Chat</div>
  ),
}));

describe("ChatWindow", () => {
  it("initially shows collapsed rail button", () => {
    render(
      <ThemeProvider>
        <ChatWindow />
      </ThemeProvider>
    );
    expect(screen.getByLabelText(/Show Chat Window/i)).toBeInTheDocument();
  });

  it("expands chat window when toggle is clicked", () => {
    render(
      <ThemeProvider>
        <ChatWindow />
      </ThemeProvider>
    );

    // Click to expand
    fireEvent.click(screen.getByLabelText(/Show Chat Window/i));

    // Should now show the chat interface
    expect(
      screen.getByRole("region", { name: /World Builder AI Assistant/i })
    ).toBeInTheDocument();
    expect(screen.getByTestId("world-builder-chat")).toBeInTheDocument();
  });

  it("collapses chat window when hide button is clicked", () => {
    render(
      <ThemeProvider>
        <ChatWindow />
      </ThemeProvider>
    );

    // Expand first
    fireEvent.click(screen.getByLabelText(/Show Chat Window/i));
    expect(screen.getByTestId("world-builder-chat")).toBeInTheDocument();

    // Then collapse
    fireEvent.click(screen.getByLabelText(/Hide Chat Window/i));
    expect(screen.queryByTestId("world-builder-chat")).not.toBeInTheDocument();
    expect(screen.getByLabelText(/Show Chat Window/i)).toBeInTheDocument();
  });

  it("has no obvious a11y violations when collapsed", async () => {
    const { container } = render(
      <ThemeProvider>
        <ChatWindow />
      </ThemeProvider>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it("has no obvious a11y violations when expanded", async () => {
    const { container } = render(
      <ThemeProvider>
        <ChatWindow />
      </ThemeProvider>
    );

    // Expand the chat
    fireEvent.click(screen.getByLabelText(/Show Chat Window/i));

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
