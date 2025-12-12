import React from "react";
import { render, screen } from "@testing-library/react";
import { ThemeProvider } from "./ThemeProvider";

describe("ThemeProvider", () => {
  it("renders children in light mode by default", () => {
    render(
      <ThemeProvider>
        <div data-testid="content">Hello</div>
      </ThemeProvider>
    );
    expect(screen.getByTestId("content")).toBeInTheDocument();
  });
});
