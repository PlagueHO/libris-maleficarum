import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { axe, toHaveNoViolations } from "jest-axe";
import { ThemeProvider } from "../../theme/ThemeProvider";
import { WorldBuilderChat } from "./WorldBuilderChat";

expect.extend(toHaveNoViolations);

// Mock CopilotChat to avoid CSS import issues with katex
vi.mock("@copilotkit/react-ui", () => ({
  CopilotChat: ({
    labels,
    className,
  }: {
    labels?: { title?: string; initial?: string };
    className?: string;
  }) => (
    <div data-testid="copilot-chat" className={className}>
      <h2>{labels?.title || "Chat"}</h2>
      <p>{labels?.initial || "Welcome"}</p>
    </div>
  ),
}));

describe("WorldBuilderChat", () => {
  it("renders the chat component with correct title", () => {
    render(
      <ThemeProvider>
        <WorldBuilderChat />
      </ThemeProvider>
    );

    expect(screen.getByTestId("copilot-chat")).toBeInTheDocument();
    expect(screen.getByText(/World Builder Assistant/i)).toBeInTheDocument();
  });

  it("displays the initial welcome message", () => {
    render(
      <ThemeProvider>
        <WorldBuilderChat />
      </ThemeProvider>
    );

    expect(
      screen.getByText(/Welcome to Libris Maleficarum/i)
    ).toBeInTheDocument();
  });

  it("has no obvious a11y violations", async () => {
    const { container } = render(
      <ThemeProvider>
        <WorldBuilderChat />
      </ThemeProvider>
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
