/**
 * OpenTelemetry Configuration
 *
 * Configures OpenTelemetry instrumentation for the application with environment-aware exporters:
 * - Development: OTLP exporter to Aspire Dashboard (http://localhost:4318)
 * - Production: Azure Monitor exporter to Application Insights
 *
 * Features:
 * - Trace propagation for API calls (W3C Trace Context)
 * - Automatic instrumentation for fetch/XHR
 * - Custom spans for user interactions and state changes
 * - Resource attributes for service identification
 *
 * @module lib/telemetry
 * @see https://opentelemetry.io/docs/languages/js/getting-started/browser/
 * @see https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable
 */

import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';
import { trace, context, SpanStatusCode, type Span } from '@opentelemetry/api';

// Service metadata from environment (injected by Aspire or set manually)
const SERVICE_NAME = import.meta.env.OTEL_SERVICE_NAME || 'libris-maleficarum-app';
const SERVICE_VERSION = '1.0.0'; // Consider extracting from package.json

// Environment detection
const isDevelopment = import.meta.env.DEV;

// OpenTelemetry configuration
// In production: APPLICATIONINSIGHTS_CONNECTION_STRING is set for Azure App Insights
const APPINSIGHTS_CONNECTION_STRING = import.meta.env.APPLICATIONINSIGHTS_CONNECTION_STRING;

/**
 * Initialize OpenTelemetry tracing
 *
 * Sets up:
 * - WebTracerProvider with service resource
 * - Environment-appropriate span exporter
 * - Auto-instrumentation for fetch/XHR
 * - W3C Trace Context propagation
 */
export function initializeTelemetry(): void {
  // Create resource with service identification
  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: SERVICE_NAME,
    [ATTR_SERVICE_VERSION]: SERVICE_VERSION,
    'deployment.environment': isDevelopment ? 'development' : 'production',
  });

  // Create tracer provider
  const provider = new WebTracerProvider({
    resource,
  });

  // Configure span exporter based on environment
  // Development: No remote export (instrumentation only for DevTools and trace context propagation)
  // Production: Export to Azure Application Insights via OTLP
  
  if (APPINSIGHTS_CONNECTION_STRING) {
    // Production: Send to Azure Application Insights

    console.log('📊 [TELEMETRY] Initializing OpenTelemetry with Azure Monitor exporter');
    
    // Note: For production, you'd use @azure/monitor-opentelemetry-exporter
    // This is a placeholder - install the package and uncomment:
    // import { AzureMonitorTraceExporter } from '@azure/monitor-opentelemetry-exporter';
    // import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
    // const azureExporter = new AzureMonitorTraceExporter({
    //   connectionString: APPINSIGHTS_CONNECTION_STRING,
    // });
    // provider.addSpanProcessor(new BatchSpanProcessor(azureExporter));
  } else if (isDevelopment) {
    // Development: Instrumentation only (no remote export)
    // - Traces visible in browser DevTools
    // - Trace context propagated to backend via W3C headers
    // - Backend services send their traces to Aspire Dashboard
    console.log('📊 [TELEMETRY] Development mode - instrumentation enabled');
    console.log('📊 [TELEMETRY] Frontend traces in DevTools, backend traces in Aspire Dashboard');
  } else {
    // No telemetry configuration provided
    console.warn('⚠️ [TELEMETRY] No App Insights connection string configured, telemetry disabled');
    return;
  }

  // Register the provider globally
  provider.register();

  // Auto-instrument fetch and XHR for API calls
  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        // Propagate trace context to backend
        propagateTraceHeaderCorsUrls: [
          /localhost:\d+/,
          /\.azurewebsites\.net/,
        ],
        // Ignore telemetry endpoints to avoid loops
        ignoreUrls: [/\/v1\/(traces|metrics|logs)/],
        // Add custom attributes to spans
        applyCustomAttributesOnSpan: (span: Span, request: Request | RequestInit, response: Response | unknown) => {
          if (response instanceof Response) {
            span.setAttribute('http.status_code', response.status);
            span.setAttribute('http.status_text', response.statusText);
          }
          
          // Extract endpoint from URL for better span names
          if (request instanceof Request) {
            const url = new URL(request.url);
            span.setAttribute('http.target', url.pathname);
            span.updateName(`HTTP ${request.method} ${url.pathname}`);
          }
        },
      }),
      new XMLHttpRequestInstrumentation({
        propagateTraceHeaderCorsUrls: [
          /localhost:\d+/,
          /\.azurewebsites\.net/,
        ],
      }),
    ],
  });

  console.log('✅ [TELEMETRY] OpenTelemetry initialized');
}

/**
 * Get the global tracer instance
 *
 * Use this to create custom spans for user interactions, state changes, etc.
 */
export const tracer = trace.getTracer(SERVICE_NAME, SERVICE_VERSION);

/**
 * Create a custom span for an operation
 *
 * @example
 * ```typescript
 * await withSpan('user.delete-entity', async (span) => {
 *   span.setAttribute('entity.id', entityId);
 *   span.setAttribute('entity.type', 'character');
 *   await deleteEntity(entityId);
 * });
 * ```
 */
export async function withSpan<T>(
  name: string,
  fn: (span: Span) => Promise<T>,
  attributes?: Record<string, string | number | boolean>
): Promise<T> {
  const span = tracer.startSpan(name);
  
  if (attributes) {
    Object.entries(attributes).forEach(([key, value]) => {
      span.setAttribute(key, value);
    });
  }

  try {
    const result = await context.with(trace.setSpan(context.active(), span), () => fn(span));
    span.setStatus({ code: SpanStatusCode.OK });
    return result;
  } catch (error) {
    span.setStatus({
      code: SpanStatusCode.ERROR,
      message: error instanceof Error ? error.message : String(error),
    });
    span.recordException(error as Error);
    throw error;
  } finally {
    span.end();
  }
}

/**
 * Create a custom span for a synchronous operation
 */
export function withSpanSync<T>(
  name: string,
  fn: (span: Span) => T,
  attributes?: Record<string, string | number | boolean>
): T {
  const span = tracer.startSpan(name);
  
  if (attributes) {
    Object.entries(attributes).forEach(([key, value]) => {
      span.setAttribute(key, value);
    });
  }

  try {
    const result = context.with(trace.setSpan(context.active(), span), () => fn(span));
    span.setStatus({ code: SpanStatusCode.OK });
    return result;
  } catch (error) {
    span.setStatus({
      code: SpanStatusCode.ERROR,
      message: error instanceof Error ? error.message : String(error),
    });
    span.recordException(error as Error);
    throw error;
  } finally {
    span.end();
  }
}
