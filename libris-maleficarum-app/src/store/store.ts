import { configureStore, createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

// Side panel slice for managing expand/collapse state
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
    setExpanded: (state, action: PayloadAction<boolean>) => {
      state.isExpanded = action.payload;
    },
  },
});

export const { toggle, setExpanded } = sidePanelSlice.actions;

// Configure the store
export const store = configureStore({
  reducer: {
    sidePanel: sidePanelSlice.reducer,
  },
});

// Export types for TypeScript
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
