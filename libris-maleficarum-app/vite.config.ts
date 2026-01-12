import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    // Proxy API requests to backend service
    // In local development with Aspire, APISERVICE_HTTPS/HTTP are auto-injected
    // In deployed environments, use VITE_API_BASE_URL
    proxy: {
      '/api': {
        target:
          process.env.APISERVICE_HTTPS ||
          process.env.APISERVICE_HTTP ||
          process.env.VITE_API_BASE_URL ||
          'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        // Keep /api prefix - backend expects it
        rewrite: (path) => path,
      },
    },
  },
})
