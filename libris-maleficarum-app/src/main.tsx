import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import { store } from './store/store'
import './index.css'
import App from './App.tsx'

/**
 * Initialize MSW for development/demo mode
 * This mocks API endpoints so the frontend works without a running backend
 */
async function initializeApp() {
  if (import.meta.env.MODE === 'development') {
    try {
      const { worker } = await import('./__mocks__/browser');
      await worker.start({
        onUnhandledRequest: 'bypass', // Allow unhandled requests to pass through to actual backend
      });
      console.log('[MSW] Mock Service Worker started in development mode');
    } catch (error) {
      console.warn('[MSW] Failed to start Mock Service Worker:', error);
      // Continue even if MSW fails - app can still work if real backend is running
    }
  }

  // Render the app after MSW is ready
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <Provider store={store}>
        <App />
      </Provider>
    </StrictMode>,
  );
}

initializeApp();
