import { configureStore } from '@reduxjs/toolkit';
// ...import your reducers here...

export const store = configureStore({
  reducer: {
    // Add your reducers here, e.g., exampleReducer: exampleSlice.reducer
  },
});

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
