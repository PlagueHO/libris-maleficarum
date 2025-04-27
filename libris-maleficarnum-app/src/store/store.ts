import { configureStore, createSlice } from '@reduxjs/toolkit';

// SidePanel slice
interface SidePanelState {
  isExpanded: boolean;
}

const initialSidePanelState: SidePanelState = {
  isExpanded: true,
};

const sidePanelSlice = createSlice({
  name: 'sidePanel',
  initialState: initialSidePanelState,
  reducers: {
    toggle: (state) => {
      state.isExpanded = !state.isExpanded;
    },
  },
});

export const { toggle } = sidePanelSlice.actions;

export const store = configureStore({
  reducer: {
    sidePanel: sidePanelSlice.reducer,
  },
});

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
