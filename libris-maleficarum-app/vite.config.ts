import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Expose to frontend whether we are running in Aspire (detected by backend service env vars)
  // This allows us to disable MSW when a real backend is available
  //
  // Also expose standard OpenTelemetry environment variables injected by Aspire AppHost.
  // When running via Aspire (e.g., `dotnet run --project AppHost`), Aspire automatically
  // sets these OTEL_* variables in the Node.js process. We expose them to the browser
  // via Vite's define() so they're accessible via import.meta.env.* in client code.
  //
  // Aspire-injected variables:
  //   OTEL_EXPORTER_OTLP_ENDPOINT - OTLP receiver (e.g., https://localhost:21001)
  //   OTEL_SERVICE_NAME - Service identifier (e.g., "frontend")
  //   OTEL_EXPORTER_OTLP_HEADERS - Optional auth headers
  //   OTEL_EXPORTER_OTLP_PROTOCOL - Wire protocol (http/protobuf or http/json)
  //   OTEL_RESOURCE_ATTRIBUTES - Resource metadata
  //
  // For production (without Aspire), set APPLICATIONINSIGHTS_CONNECTION_STRING instead.
  define: {
    'import.meta.env.VITE_HAS_ASPIRE_BACKEND': JSON.stringify(
      !!(process.env.services__api__https__0 || process.env.services__api__http__0)
    ),
    // OpenTelemetry configuration (automatically set by Aspire AppHost)
    'import.meta.env.OTEL_EXPORTER_OTLP_ENDPOINT': JSON.stringify(
      process.env.OTEL_EXPORTER_OTLP_ENDPOINT || ''
    ),
    'import.meta.env.OTEL_SERVICE_NAME': JSON.stringify(
      process.env.OTEL_SERVICE_NAME || 'libris-maleficarum-app'
    ),
    'import.meta.env.OTEL_EXPORTER_OTLP_HEADERS': JSON.stringify(
      process.env.OTEL_EXPORTER_OTLP_HEADERS || ''
    ),
    'import.meta.env.OTEL_EXPORTER_OTLP_PROTOCOL': JSON.stringify(
      process.env.OTEL_EXPORTER_OTLP_PROTOCOL || 'http/protobuf'
    ),
    'import.meta.env.OTEL_RESOURCE_ATTRIBUTES': JSON.stringify(
      process.env.OTEL_RESOURCE_ATTRIBUTES || ''
    ),
    // Production: Azure Application Insights connection string
    'import.meta.env.APPLICATIONINSIGHTS_CONNECTION_STRING': JSON.stringify(
      process.env.APPLICATIONINSIGHTS_CONNECTION_STRING || process.env.VITE_APPLICATIONINSIGHTS_CONNECTION_STRING || ''
    ),
  },
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
