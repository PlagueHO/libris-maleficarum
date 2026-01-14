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
    proxy: {
      '/api/v1': {
        target:
          process.env.services__api__https__0 ||
          process.env.services__api__http__0 ||
          process.env.VITE_API_BASE_URL ||
          'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        // Keep /api/v1 prefix - backend expects it
        rewrite: (path) => path,
      },
    },
  },
})
