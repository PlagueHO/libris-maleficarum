import { configureStore, createSlice } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import { useDispatch, useSelector } from 'react-redux';
import type { TypedUseSelectorHook } from 'react-redux';
import { api } from '@/services/api';
import worldSidebarReducer from './worldSidebarSlice';
import notificationsReducer from './notificationsSlice';

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
    worldSidebar: worldSidebarReducer,
    notifications: notificationsReducer,
    // Add RTK Query API reducer
    [api.reducerPath]: api.reducer,
  },
  // Add RTK Query middleware for automatic refetching and cache management
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

// Export types for TypeScript
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

// Export typed hooks
export const useAppDispatch: () => AppDispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
