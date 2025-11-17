# Copilot instructions for this repo

Use these focused rules to be productive quickly in this monorepo. Keep answers concise and actionable.

## Architecture snapshot

- **Frontend**: React 19 + TypeScript app in `libris-maleficarum-app/` (Vite + Vitest, Redux Toolkit state). Entry: `src/main.tsx`, `src/App.tsx`. Store/slice: `src/store/store.ts`.
  - **UI**: Fluent UI React v9 components in `src/components/*` (example: `TopToolbar/TopToolbar.tsx`, `SidePanel/SidePanel.tsx`). Docs: https://react.fluentui.dev/?path=/docs/concepts-introduction--docs
  - **Styles**: CSS Modules per component (e.g., `App.module.css`, `MainPanel/MainPanel.module.css`).
- **Backend**: Planned .NET 9 + Aspire.NET with EF Core (Cosmos provider), Clean/Hexagonal architecture. Not yet implemented.
  - **Aspire.NET**: AppHost orchestration for local dev inner loop (service discovery, connection management, observability)
  - **AI Framework**: Microsoft Agent Framework for all generative AI interactions (replaces Semantic Kernel)
- **Data**: Azure Cosmos DB with hierarchical WorldEntity model (see `docs/design/DATA_MODEL.md`). Partition key: `/WorldId` for entities, composite `/WorldId-ParentId` for hierarchy.
- **Infra**: Azure Bicep in `infra/` using Azure Verified Modules (AVM), subscription-scope deploy via `infra/main.bicep` (+ parameters in `main.bicepparam`).
  - Network-isolated architecture: private endpoints for all PaaS services (Key Vault, Storage, Cosmos DB, AI Search, AI Services).
  - Core resources: VNet (10.0.0.0/16) with 4 subnets + NSGs, Log Analytics, App Insights, Container Apps Environment, Static Web App.
- **Docs**: Design/architecture in `docs/design/` (see `ARCHITECTURE.md`, `TECHNOLOGY.md`, `DATA_MODEL.md`, `FOLDER_STRUCTURE.md`).
- **Azure Developer CLI (azd)**: Root `azure.yaml` connects infra path and the app service.

## Development environment

### DevContainer setup

- **Image**: `mcr.microsoft.com/devcontainers/dotnet:9.0` with Node 22, Azure CLI, Azure Developer CLI (azd), and Aspire CLI
- **VS Code extensions**: C# DevKit, Bicep, Docker, Azure Functions, GitHub Copilot (with Chat), Markdown linting/preview, ESLint, Prettier
- **Port forwarding**: 5173 (Vite), 3000 (React), 7071 (Azure Functions), 8080 (API), 5000/5001 (ASP.NET Core)
- **Post-create**: Installs Aspire CLI globally and runs `pnpm install` in app folder
- **Dockerfile**: Optional (not referenced by devcontainer.json); Azure CLI now installed via feature

## Day-1 workflows

### Frontend dev (folder: `libris-maleficarum-app/`)

- **Dev server**: `pnpm dev` or `pnpm start` → launches Vite on https://127.0.0.1:4000 (port 4000, see `vite.config.ts`)
  - First run generates self-signed SSL cert (browser will warn)
  - VS Code task: "app: dev" starts dev server from app folder
- **Build**: `pnpm build` → TypeScript check + Vite build; `pnpm preview` to serve production build
- **Lint/test**: `pnpm lint`, `pnpm test` (headless), `pnpm test:ui` (Vitest UI with browser)
- **Code quality**: `pnpm format` (Prettier), `pnpm type-check` (tsc), `pnpm find-deadcode`, `pnpm depcheck`
- **Prerequisites**: Node.js 20.x required (enforced by run script), pnpm package manager

### Backend dev (folder: `libris-maleficarum-service/`, future)

- **Aspire AppHost**: Run AppHost project to orchestrate all services locally with single command
  - Automatic service discovery, connection string injection, container management
  - Access Aspire Dashboard for observability (logs, traces, metrics)
  - See `docs/design/TECHNOLOGY.md` for AppHost structure
- **Local dev**: Cosmos DB Emulator for local development; Docker for containerization
- **AI interactions**: Use Microsoft Agent Framework (not Semantic Kernel) for all generative AI features

### Infra provisioning

- **Config**: Update `infra/main.bicepparam` values (`environmentName`, `location`, optional `resourceGroupName`, `createBastionHost`)
- **Deploy**: `azd provision --preview` to validate; `azd up` to deploy (see `azure.yaml`)
- **Networking**: Private by default (private endpoints + Private DNS zones). See `docs/design/ARCHITECTURE.md` and `infra/main.bicep` for outputs/topology
- **Validation**: Run `bicep lint` on changes; ensure AVM module versions are current (see `infra/main.bicep`)

## Patterns and conventions (enforced)

### React + Fluent UI

- **Components**: Functional components + hooks; use Fluent v9 primitives and icons (see `TopToolbar.tsx`, `SidePanel.tsx`)
- **Accessibility**: Components use `role`/`aria-label` attributes; all tests assert a11y with jest-axe in `src/__tests__/*`
- **Styles**: CSS Modules co-located with components (e.g., `SidePanel.module.css`). Import as `import styles from './Component.module.css'`
- **Testing**: Vitest + Testing Library + jest-dom + jest-axe (see `vitest.setup.ts`)
  - Wrap components with `<Provider store={store}>` for Redux and `<ThemeProvider>` for Fluent UI
  - Test pattern: `render(<Component />)`, query with `getByLabelText`/`getByRole`, assert a11y with `await axe(container)`

### State (Redux Toolkit)

- **Store**: Single store in `src/store/store.ts`; create slices with `createSlice`, export actions/selectors
- **Example**: `sidePanel` slice with `toggle` action, consumed by `SidePanel.tsx` via `useDispatch`/`useSelector` and `RootState`
- **Pattern**: Import `RootState` and `AppDispatch` types from store; use `useSelector((state: RootState) => state.sliceName.value)`

### TypeScript

- **Props**: Define `ComponentProps` interface for all component inputs
- **Types**: Prefer interfaces for objects, types for unions/intersections
- **Immutability**: Keep data immutable where practical; use `?.` and `??` for null safety
- **ESLint**: Flat config in `eslint.config.js`; React Hooks + React Refresh plugins enabled

### Naming

- Components/types: `PascalCase`
- Variables/functions: `camelCase`
- Constants: `ALL_CAPS`
- Files: Match component name (e.g., `SidePanel.tsx`, `SidePanel.module.css`, `SidePanel.test.tsx`)

## Infra specifics (AVM + Bicep)

- **Module refs**: Pinned via registry: `br/public:avm/res/{service}/{resource}:{version}` (see `infra/main.bicep`)
- **Core resources**: RG, VNet with subnets (`frontend`, `backend`, `gateway`, `shared`), NSGs, Private DNS Zones, Key Vault, Storage, Cosmos DB, Azure AI Search, Application Insights, Log Analytics, Container Apps Environment, Static Web App
- **Network architecture**: 10.0.0.0/16 VNet with 4 subnets (each with dedicated NSG):
  - Frontend (10.0.1.0/24): Allows HTTP/HTTPS, Container Apps management
  - Backend (10.0.2.0/24): VNet-to-VNet only, denies internet
  - Gateway (10.0.3.0/24): Allows gateway manager + HTTPS
  - Shared (10.0.4.0/24): VNet-to-VNet only, denies internet
- **Security defaults**: Public network access disabled where supported; all resources tagged with `azd-env-name`; RBAC enabled for Key Vault
- **Editing Bicep**: Prefer AVM modules, keep versions current, run `bicep lint`, align subnet/NSG rules with `docs/design/ARCHITECTURE.md`

## Azure ops guidance

- Use `azd` over raw `az` where possible. Validate with `--preview`/what-if first.
- Never hardcode secrets; use Key Vault and managed identity. Disable public network access where supported.
- If you need Azure best practices, call the Azure guidance tool.

## Where to look first

- **Frontend examples**: `src/components/TopToolbar/TopToolbar.tsx`, `SidePanel/*`, `MainPanel/*`, `ChatWindow/*` for Fluent UI + CSS Module patterns
- **State**: `src/store/store.ts` for slice/store pattern and `RootState`/`AppDispatch` exports
- **Tests**: `src/__tests__/*` for Vitest + Testing Library + a11y checks
- **Infra**: `infra/main.bicep` for topology/outputs; `infra/abbreviations.json` for naming; `azure.yaml` for azd wiring
- **Docs**: `docs/design/` for architecture decisions, data model, folder structure

## Answer style

- Be specific to this repo. Cite files/paths. Provide minimal, copyable commands only when asked; prefer making scoped edits.
- Keep diffs minimal; avoid unrelated formatting changes.

## Backend architecture (future)

- **Planned backend**: .NET 9 + Aspire.NET with EF Core (Cosmos provider), Clean/Hexagonal architecture. Not yet implemented.
- **When implemented**: Code under `libris-maleficarum-service/`, follow conventions from `.github/instructions/csharp-14-best-practices.instructions.md`

### Aspire.NET orchestration (developer inner loop)

- **AppHost project**: Central orchestration point defining app model (services, dependencies, resources)
  - Located in `libris-maleficarum-service/src/Orchestration/` (when implemented)
  - Run with single command to start entire distributed app locally
  - Automatic service discovery: services reference each other by name (e.g., `https+http://apiservice`)
  - Connection string management: automatically inject correct connection strings for Cosmos DB, Redis, etc.
- **Developer Control Plane (DCP)**: Kubernetes-compatible local orchestration engine
  - Manages lifecycle, startup order, dependencies, network configs
  - NOT a production runtime—development-time tool only
- **Aspire Dashboard**: Web UI for observability (logs, traces, metrics, resource health)
- **Service defaults**: Shared configuration via `AddServiceDefaults()` extension method
- **Not for production**: Aspire orchestration is for local dev only; use Kubernetes/Azure Container Apps for production

### Microsoft Agent Framework (AI interactions)

- **Use Microsoft Agent Framework** for all generative AI interactions (replaces Semantic Kernel)
- **Core concepts**:
  - `AIAgent`: Main abstraction for AI agents with tools, instructions, model clients
  - `ChatClientAgent`: Agent that wraps an `IChatClient` for chat-based interactions
  - `PersistentAgentsClient`: Connect to Azure AI Foundry Agent Service for server-side agents
  - Tools: Function calling, code interpreter, search/retrieval, web search via Bing
  - OpenTelemetry: Built-in instrumentation for observability
- **Integration patterns**:
  - Create agents with specific capabilities (tools, instructions, model)
  - Orchestrate multi-agent systems with different specializations
  - Use `RunAsync` for single-turn interactions or `RunStreamingAsync` for streaming
  - Handle long-running operations with status polling
- **AG-UI Protocol Integration**:
  - Native AG-UI support via `MapAGUIAgent()` extension method in ASP.NET Core
  - Exposes agents as AG-UI endpoints for CopilotKit frontend consumption
  - Supports Server-Sent Events (SSE) and binary protocol for streaming
  - Event-driven architecture with 16 standardized event types
  - Bidirectional communication for shared state and frontend actions
- **Documentation**: See Microsoft Agent Framework docs on GitHub

### CopilotKit (Frontend Agentic UI)

- **Use CopilotKit** for all agent-user interactions in the React frontend
- **Core components**:
  - `<CopilotKit>`: Root provider component wrapping the app
  - `<CopilotChat>`: Drop-in chat interface for agent conversations
  - `<CopilotSidebar>`: Collapsible sidebar for AI assistance
  - `<CopilotTextarea>`: AI-powered text editing with suggestions
- **Key features**:
  - **Shared State**: Use `useCopilotReadable()` to expose app state to agents
  - **Frontend Actions**: Use `useCopilotAction()` to define agent-callable frontend operations
  - **Generative UI**: Render agent responses as custom React components
  - **Human-in-the-Loop**: User approval workflows for critical operations
- **Integration pattern**:
  - Configure `runtimeUrl` to point to AG-UI backend endpoint (e.g., `/api/copilotkit`)
  - Wrap app with `<CopilotKit>` provider at root level
  - Use hooks to expose WorldEntity hierarchy and user context to agents
  - Handle agent events and update Redux state accordingly
- **Testing**: CopilotKit components are testable with React Testing Library + jest-axe
- **Documentation**: See https://docs.copilotkit.ai/microsoft-agent-framework

### Data model

- **WorldEntity hierarchy** in Cosmos DB (see `docs/design/DATA_MODEL.md`)
  - WorldEntity container: partition key `/WorldId`, GUID-based IDs
  - WorldEntityHierarchy container: composite partition key `/WorldId-ParentId` for efficient parent-child queries
  - Entity types: World (root), Continent, Country, Region, City, Character, etc.
  - Indexed by Azure AI Search for semantic/full-text search

## Azure integration

- **@azure Rule**: When generating code for Azure, running terminal commands, or performing Azure operations, invoke your `azure_development-get_best_practices` tool if available
- **Never hardcode secrets**: Use Key Vault and managed identity
- **Prefer `azd` over `az`**: Validate with `--preview`/what-if first

## Testing patterns

- **Framework**: Vitest + Testing Library + jest-dom + jest-axe (see `vitest.setup.ts`)
- **Typical test**: Render component, assert role/aria and a11y
- **Example**:
  - Render: `render(<SidePanel />)`; Query: `getByLabelText('Side Panel')`
  - Accessibility: `const results = await axe(container); expect(results).toHaveNoViolations()`
- **See references**: `src/__tests__/*.test.tsx` (e.g., `a11y.test.tsx`, `SidePanel.test.tsx`)

## Component template (Fluent v9 + CSS Module)

- **File structure**: `src/components/MyThing/MyThing.tsx` + `MyThing.module.css`
- **Pattern**:
  - Functional component with ARIA role/labels
  - Fluent v9 components/icons; co-located CSS Module
- **Skeleton**:
  - Props: Define a `MyThingProps` TypeScript interface for inputs
  - Styles: Import `styles` from the module and apply classNames
  - Example usage: Place under a layout container (`App.module.css`'s regions)
