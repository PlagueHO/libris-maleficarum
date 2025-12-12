import React from "react";
import { render, screen } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { expect, it, describe } from "vitest";
import { ThemeProvider } from "../../theme/ThemeProvider";
import MainPanel from "./MainPanel";

expect.extend(toHaveNoViolations);

describe("MainPanel", () => {
  it("renders MainPanel heading", () => {
    render(<MainPanel />);
    expect(
      screen.getByRole("heading", { name: /libris maleficarum/i })
    ).toBeInTheDocument();
  });

  it("has no obvious a11y violations", async () => {
    const { container } = render(
      <ThemeProvider>
        <MainPanel />
      </ThemeProvider>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
