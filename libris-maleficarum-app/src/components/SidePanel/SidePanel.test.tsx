import React from "react";
import { Provider } from "react-redux";
import { render, screen } from "@testing-library/react";
import { store } from "../../store/store";
import SidePanel from "./SidePanel";

it("renders SidePanel navigation title", () => {
  render(
    <Provider store={store}>
      <SidePanel />
    </Provider>
  );
  expect(screen.getByText(/Navigation/i)).toBeInTheDocument();
});
