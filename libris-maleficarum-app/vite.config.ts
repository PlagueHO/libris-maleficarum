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
    // In local development with Aspire, environment variables follow convention:
    // services__<servicename>__<protocol>__<index>
    // For service named "api": services__api__https__0, services__api__http__0
    // In deployed environments, use VITE_API_BASE_URL
    proxy:
      // Disable proxy if using MSW for development (detected by VITE_API_BASE_URL=http://localhost:5000)
      process.env.VITE_API_BASE_URL === 'http://localhost:5000'
        ? {} // Empty proxy config lets MSW intercept directly
        : {
            '/api/v1': {
              target:
                process.env.services__api__https__0 ||
                process.env.services__api__http__0 ||
                process.env.VITE_API_BASE_URL ||
                'http://localhost:5077', // Point to Aspire backend
              changeOrigin: true,
              secure: false,
              // Keep /api/v1 prefix - backend expects it
              rewrite: (path) => path,
            },
          },
  },
})
