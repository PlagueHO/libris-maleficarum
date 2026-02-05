# Telemetry and Logging Guide

## Overview

The application uses **OpenTelemetry** for distributed tracing and structured logging with environment-aware configuration:

- **Local Development**: Instrumentation only (traces in browser DevTools, trace context propagation to backend)
- **Production**: OTLP export to Azure Application Insights

## Architecture

**Two-Mode Approach:**

**Local Development (with Aspire):**
- **Frontend (browser)**: OpenTelemetry instrumentation only
  - Traces visible in browser DevTools
  - W3C Trace Context headers propagated to backend (for distributed tracing)
  - **No remote export** (CORS prevents browser → Aspire OTLP)
- **Backend services**: OTLP export to Aspire Dashboard (gRPC, no CORS issues)
  - View traces: http://localhost:18888 (Aspire Dashboard)
  - Receives correlated traces from frontend via trace context headers

**Production (Azure):**
- **Frontend**: OTLP export to Azure Application Insights
  - Azure endpoints configured with CORS headers
  - Full distributed tracing
- **Backend services**: OTLP export to Azure Application Insights
- Distributed traces correlated via W3C Trace Context headers

**Why frontend doesn't export to Aspire:**

Browsers cannot send OTLP (HTTP/protobuf or gRPC-Web) to Aspire's OTLP endpoint due to CORS restrictions. Aspire's OTLP endpoint runs on a different origin (https://localhost:21001) and doesn't expose CORS headers (it's designed for backend-to-backend communication). This is expected and correct - the frontend instrumentation still provides value via DevTools visibility and trace context propagation.

```
┌─────────────────────────┐
│  React App (Browser)    │
│  OpenTelemetry Web SDK  │
└────────┬────────────────┘
         │
         │ W3C Trace Context (traceparent header)
         │
    ┌────▼────────────────────────────┐
    │                                 │
    │  Local Dev       Production     │
    │  ─────────       ──────────     │
    │  DevTools        Azure Monitor  │
    │  (no export)     OTLP Exporter  │
    │                                 │
    └──────────────────────┬──────────┘
                           │
                      ┌────▼──────────────────┐
                      │  Application Insights │
                      │  (Azure)              │
                      └───────────────────────┘

Backend Services (ASP.NET Core):
┌──────────────────┐
│  API / Services  │
└────────┬─────────┘
         │ OTLP (gRPC)
         │
    ┌────▼────────────────────────┐
    │                             │
    │  Local Dev    Production    │
    │  ─────────    ──────────    │
    │  Aspire       Application   │
    │  Dashboard    Insights      │
    │  :18888       (Azure)       │
    └─────────────────────────────┘
```

## Quick Start

### 1. Install Dependencies

```bash
pnpm install
```

This installs the required OpenTelemetry packages (see [package.json](package.json)):
- `@opentelemetry/api` - Core API
- `@opentelemetry/sdk-trace-web` - Browser tracing SDK
- `@opentelemetry/instrumentation-fetch` - Auto-instrument fetch API
- `@opentelemetry/instrumentation-xml-http-request` - Auto-instrument XHR

**Note**: OTLP exporter is only needed for production (Azure Monitor). Development mode uses instrumentation only.

### 2. Configure Environment Variables

**Local Development:**

No configuration needed! OpenTelemetry instrumentation works out-of-the-box.

**Production (Azure):**

Set the Application Insights connection string:

```bash
# Azure Static Web App or Container App environment variables
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=https://region.applicationinsights.azure.com/
```

### 3. Start Aspire Dashboard

The Aspire AppHost automatically starts the dashboard on port 18888:

```bash
cd ../libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

Access dashboard at: http://localhost:18888

**Note**: Only backend service traces appear in Aspire Dashboard. Frontend traces are visible in browser DevTools.

### 4. Run Application

```bash
pnpm dev
```

**View Traces:**
- **Backend traces**: http://localhost:18888 → Traces tab (Aspire Dashboard)
- **Frontend traces**: Browser DevTools → Console (search for "Span" objects)
- **API calls**: Automatically instrumented with HTTP details

## Usage

### Structured Logging

Use the centralized logger for all console output:

```typescript
import { logger } from '@/lib/logger';

// Categories: 'API' | 'STATE' | 'UI' | 'AUTH' | 'PERF' | 'TELEMETRY'

// Simple log
logger.info('UI', 'User clicked button');

// With context object
logger.error('API', 'Failed to fetch entities', {
  worldId,
  error,
});

// Convenience methods
logger.apiRequest('GET', '/api/v1/worlds');
logger.apiResponse('/api/v1/worlds', 200, { count: 5 });
logger.userAction('Delete entity', { entityId });
logger.stateChange('openDeleteConfirmation', { entityId });
logger.performance('Entity tree render', 12, 'ms');
```

**Output Format:**
```
🌐 [API] GET /api/v1/worlds
🎨 [UI] User clicked "Delete Entity" { entityId: "abc-123" }
🔄 [STATE] openDeleteConfirmation { entityId: "abc-123" }
⚡ [PERF] Entity tree render: 12ms
```

### Custom Spans

Create custom spans for important operations:

```typescript
import { withSpan } from '@/lib/telemetry';

async function deleteEntity(entityId: string) {
  await withSpan('user.delete-entity', async (span) => {
    span.setAttribute('entity.id', entityId);
    span.setAttribute('entity.type', 'character');
    
    // Your async operation
    await apiClient.delete(`/entities/${entityId}`);
  });
}
```

### Automatic Instrumentation

Fetch and XHR are automatically instrumented:

```typescript
// This automatically creates a span with HTTP details
const response = await fetch('/api/v1/worlds');

// Span attributes added automatically:
// - http.method: GET
// - http.url: /api/v1/worlds
// - http.status_code: 200
// - http.target: /api/v1/worlds
```

### Trace Context Propagation

W3C Trace Context headers are automatically added to API requests:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
tracestate: vendor1=value1,vendor2=value2
```

This allows backend services to correlate frontend and backend traces.

## Best Practices

### What to Log

**✅ DO:**
- User interactions (button clicks, form submissions)
- API requests/responses (in development)
- State changes (Redux actions)
- Errors with context
- Performance metrics

**❌ DON'T:**
- Every component render
- Routine data transformations
- Sensitive data (passwords, tokens, PII)
- Verbose object dumps
- In production (except errors/warnings)

### Log Levels

- **`debug`**: Detailed diagnostic info (dev only)
- **`info`**: Important events (dev only)
- **`warn`**: Unexpected but recoverable (always)
- **`error`**: Errors requiring attention (always)

### Context Objects

Always include relevant context:

```typescript
// ✅ Good - specific context
logger.error('API', 'Failed to create entity', {
  worldId,
  entityType: 'character',
  parentId,
  error,
});

// ❌ Bad - vague, no context
logger.error('API', 'Something went wrong');
```

## Production Setup

### Azure Application Insights

1. **Install Azure Monitor exporter:**

```bash
pnpm add @azure/monitor-opentelemetry-exporter
```

2. **Uncomment Azure exporter in telemetry.ts:**

```typescript
// In src/lib/telemetry.ts, uncomment:
import { AzureMonitorTraceExporter } from '@azure/monitor-opentelemetry-exporter';

const azureExporter = new AzureMonitorTraceExporter({
  connectionString: APPINSIGHTS_CONNECTION_STRING,
});
provider.addSpanProcessor(new BatchSpanProcessor(azureExporter));
```

3. **Set connection string in production environment:**

```bash
# Azure Static Web App or Container App environment variables
VITE_APPINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx;IngestionEndpoint=https://region.applicationinsights.azure.com/
```

4. **View traces in Azure Portal:**

Navigate to: Application Insights → Investigate → Performance → Traces

## Aspire Dashboard Features

### Traces Tab

- View distributed traces across frontend and backend
- Filter by service, operation, duration
- Drill into individual spans
- See span attributes and events

### Metrics Tab

- View performance counters
- Monitor HTTP request rates
- Track error rates

### Logs Tab

- Structured log viewing
- Filter by severity, service
- Search log messages

### Resources Tab

- Service topology
- Health checks
- Resource utilization

## Troubleshooting

### Traces Not Appearing in Aspire

**Expected Behavior:**
- **Backend traces**: Should appear in Aspire Dashboard
- **Frontend traces**: Only visible in browser DevTools (CORS limitation)

1. **Check Aspire Dashboard is running:**
   ```bash
   # Verify dashboard is accessible
   curl http://localhost:18888
   ```

2. **Check browser console for telemetry initialization:**
   ```
   📊 [TELEMETRY] Development mode - instrumentation enabled
   📊 [TELEMETRY] Frontend traces in DevTools, backend traces in Aspire Dashboard
   ✅ [TELEMETRY] OpenTelemetry initialized
   ```

3. **View frontend spans in browser:**
   - Open DevTools → Console
   - Look for span objects logged during API calls
   - Each span shows timing, attributes, and events

### Logger Not Working

1. **Check development mode:**
   ```typescript
   console.log(import.meta.env.DEV); // Should be true in dev
   ```

2. **Verify logger import:**
   ```typescript
   import { logger } from '@/lib/logger'; // ✅ Correct
   import { logger } from './lib/logger'; // ❌ Wrong
   ```

### No CORS Errors Expected

**You should NOT see CORS errors** in development mode because the frontend doesn't attempt to export to Aspire's OTLP endpoint.

If you previously saw CORS errors for `https://localhost:21001`, these have been eliminated by removing OTLP export in development mode.

**Current behavior:**
- **Development**: No remote export, no CORS issues
- **Production**: Export to Azure Application Insights (CORS headers configured)

## References

- [OpenTelemetry JavaScript Documentation](https://opentelemetry.io/docs/languages/js/)
- [OpenTelemetry Browser Guide](https://opentelemetry.io/docs/languages/js/getting-started/browser/)
- [Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)
- [Aspire Dashboard Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/)
- [W3C Trace Context Specification](https://www.w3.org/TR/trace-context/)
