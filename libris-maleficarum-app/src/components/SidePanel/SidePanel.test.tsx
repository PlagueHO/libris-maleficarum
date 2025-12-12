import React from "react";
import { Provider } from "react-redux";
import { render, screen } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { expect, it, describe } from "vitest";
import { ThemeProvider } from "../../theme/ThemeProvider";
import { store } from "../../store/store";
import SidePanel from "./SidePanel";

expect.extend(toHaveNoViolations);

describe("SidePanel", () => {
  it("renders SidePanel navigation title", () => {
    render(
      <Provider store={store}>
        <SidePanel />
      </Provider>
    );
    expect(screen.getByText(/Navigation/i)).toBeInTheDocument();
  });

  it("has no obvious a11y violations", async () => {
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
});
