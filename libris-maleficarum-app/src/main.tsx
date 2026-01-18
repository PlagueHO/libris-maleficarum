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
  // Only use MSW if we are in development and NOT connecting to a real backend (Aspire or explicit URL).
  // VITE_HAS_ASPIRE_BACKEND is injected by vite.config.ts via 'define' based on Aspire env vars.
  const isBackendConfigured =
    import.meta.env.VITE_HAS_ASPIRE_BACKEND ||
    (import.meta.env.VITE_API_BASE_URL && import.meta.env.VITE_API_BASE_URL !== 'http://localhost:5000');

  if (import.meta.env.MODE === 'development' && !isBackendConfigured) {
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
