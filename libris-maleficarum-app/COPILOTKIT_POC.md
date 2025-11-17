# CopilotKit Proof-of-Concept

This directory contains a proof-of-concept integration of CopilotKit with AG-UI protocol support for Libris Maleficarum.

## Components

### WorldBuilderChat
`src/components/WorldBuilderChat/WorldBuilderChat.tsx`

A chat interface component powered by CopilotKit that demonstrates:
- Integration with Fluent UI v9 theming
- AG-UI protocol readiness (currently using mock endpoint)
- Customizable chat labels and styling

### useCopilotWorldContext Hook
`src/hooks/useCopilotWorldContext.ts`

A custom React hook that demonstrates:
- **Shared State**: Exposing Redux state to CopilotKit agents via `useCopilotReadable()`
- **Frontend Actions**: Defining agent-callable actions via `useCopilotAction()`
- **Mock WorldEntity Data**: Placeholder world hierarchy that will be replaced with Cosmos DB data

## Current State (POC)

### ✅ Implemented
- CopilotKit packages installed (`@copilotkit/react-core`, `@copilotkit/react-ui`)
- CopilotKit provider wrapping the app in `App.tsx`
- WorldBuilderChat component with Fluent UI v9 styling
- Shared state hook exposing Redux state to agents
- Frontend action example (createWorldEntity)
- Mock endpoint configuration (demo mode)
- Accessibility testing setup with jest-axe

### ⏳ Pending Backend Implementation
- Microsoft Agent Framework backend (.NET 9)
- AG-UI protocol endpoint (`/api/copilotkit`)
- Azure Cosmos DB integration for WorldEntity hierarchy
- Real agent tools (function calling, code interpreter, search)
- Azure AI Foundry integration
- OpenTelemetry observability

## How It Works (Current POC)

1. **App.tsx**: Wraps the entire app with `<CopilotKit>` provider
   - Currently configured with mock endpoint
   - Will point to `/api/copilotkit` once backend is ready

2. **ChatWindow.tsx**: Integrates WorldBuilderChat component
   - Collapsible chat panel on the right
   - Toggle button for show/hide

3. **useCopilotWorldContext.ts**: Exposes app context to agents
   - Uses `useCopilotReadable()` to share Redux state
   - Uses `useCopilotAction()` to define callable actions
   - Demonstrates bidirectional communication pattern

4. **WorldBuilderChat.tsx**: Renders the chat interface
   - Uses CopilotKit's `<CopilotChat>` component
   - Customized labels and styling
   - Themed with Fluent UI v9 design tokens

## Testing

```bash
# Run all tests including WorldBuilderChat
pnpm test

# Run tests in UI mode
pnpm test:ui
```

The WorldBuilderChat component includes accessibility tests using jest-axe to ensure compliance with WCAG standards.

## Next Steps

### Phase 1: Backend Setup
1. Create .NET 9 backend with Aspire.NET
2. Add Microsoft Agent Framework packages
3. Implement AG-UI endpoint with `MapAGUIAgent()`
4. Configure Azure AI Foundry connection

### Phase 2: WorldEntity Integration
1. Implement WorldEntity repository with EF Core + Cosmos DB
2. Create world-building agents with Microsoft Agent Framework
3. Add agent tools for CRUD operations on entities
4. Implement agent-driven world generation

### Phase 3: Advanced Features
1. Add Human-in-the-Loop approvals for critical operations
2. Implement Generative UI for dynamic entity visualizations
3. Add multi-agent orchestration for complex tasks
4. Enable voice input via CopilotKit audio support

## Configuration

### Current (POC Mode)
```tsx
<CopilotKit
  runtimeUrl="https://mock-agent.example.com/api/copilotkit"
  agent="world-builder-poc"
  publicApiKey="demo-mode"
>
```

### Future (Production)
```tsx
<CopilotKit
  runtimeUrl="/api/copilotkit"
  agent="world-builder"
  // Authentication will be handled by Azure Entra ID
>
```

## Architecture

```
┌─────────────────────────────────────────────┐
│ Frontend (React 19 + TypeScript)            │
│ ├── CopilotKit Provider                     │
│ ├── Redux Store (shared state)              │
│ ├── WorldBuilderChat (UI)                   │
│ └── useCopilotWorldContext (shared state)   │
└─────────────────────────────────────────────┘
                    ↕ AG-UI Protocol (POC: mock, Future: /api/copilotkit)
┌─────────────────────────────────────────────┐
│ Backend (Future: .NET 9 + Aspire.NET)      │
│ ├── Microsoft Agent Framework               │
│ ├── AG-UI Server (MapAGUIAgent)            │
│ ├── WorldEntity Repository (EF Core)        │
│ └── Azure Cosmos DB                          │
└─────────────────────────────────────────────┘
```

## Resources

- [CopilotKit Documentation](https://docs.copilotkit.ai/)
- [CopilotKit + Microsoft Agent Framework](https://docs.copilotkit.ai/microsoft-agent-framework)
- [AG-UI Protocol](https://docs.ag-ui.com/)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)
