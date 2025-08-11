# Copilot instructions for this repo

Use these focused rules to be productive quickly in this monorepo. Keep answers concise and actionable.

## Architecture snapshot

- Frontend: React + TypeScript app in `libris-maleficarum-app/` (Vite + Vitest, Redux Toolkit state). Entry: `src/main.tsx`, `src/App.tsx`. Store/slice: `src/store/store.ts`.
  - UI: Fluent UI React v9 components in `src/components/*` (example: `TopToolbar/TopToolbar.tsx`, `SidePanel/SidePanel.tsx`). Docs: https://react.fluentui.dev/?path=/docs/concepts-introduction--docs
  - Styles: CSS Modules per component (e.g., `App.module.css`, `MainPanel/MainPanel.module.css`).
- Infra: Azure Bicep in `infra/` using Azure Verified Modules (AVM), subscription-scope deploy via `infra/main.bicep` (+ parameters in `main.bicepparam`).
- Docs: Design/architecture in `docs/design/` (see `ARCHITECTURE.md`, `TECHNOLOGY.md`, `FOLDER_STRUCTURE.md`).
- Azure Developer CLI (azd): root `azure.yaml` connects infra path and the app service.

## Day-1 workflows

- Frontend dev (folder: `libris-maleficarum-app/`)
  - `pnpm dev` (or `pnpm start`) → launches Vite on http://127.0.0.1:4000 (see `vite.config.ts`)
  - `pnpm build` → type-check + build; `pnpm preview` to serve the build
  - Lint/tests: `pnpm lint`, `pnpm test` (headless), `pnpm test:ui` (Vitest UI)
  - VS Code task: “app: dev” starts the dev server from the app folder
- Infra provisioning
  - Update `infra/main.bicepparam` values (`environmentName`, `location`, optional `resourceGroupName`, `createBastionHost`).
  - Validate with `azd provision --preview`; deploy with `azd up` (see `azure.yaml`).
  - Networking is private by default (private endpoints + Private DNS). See `docs/design/ARCHITECTURE.md` and `infra/main.bicep` for outputs and topology.

## Patterns and conventions (enforced)

- React + Fluent UI
  - Functional components + hooks; use Fluent v9 primitives and icons (see `TopToolbar.tsx`, `SidePanel.tsx`).
  - Accessibility: components use roles/aria labels; tests assert accessibility in `src/__tests__/*` (Vitest + Testing Library + jest-axe).
  - Styles via CSS Modules co-located with components (e.g., `*.module.css`).
- State (Redux Toolkit)
  - Single store in `src/store/store.ts`; create slices with `createSlice` and export actions/selectors. Example: `sidePanel` slice with `toggle` action, consumed by `SidePanel.tsx` via `useDispatch`/`useSelector` and `RootState`.
- TypeScript
  - Prefer interfaces/types for props; keep data immutable where practical; use `?.` and `??`.
- Naming
  - Components/types: PascalCase; vars/functions: camelCase; constants: ALL_CAPS.

## Infra specifics (AVM + Bicep)

- Modules pinned via registry refs: `br/public:avm/res/{service}/{resource}:{version}` (see `infra/main.bicep`).
- Core resources: RG, VNet with subnets (`frontend`, `backend`, `gateway`, `shared`), NSGs, Private DNS Zones, Key Vault, Storage, Cosmos DB, Azure AI Search, Application Insights, Log Analytics, Container Apps Environment, Static Web App.
- Defaults: public network access disabled where supported; all resources tagged with `azd-env-name`.
- Editing Bicep: prefer AVM modules, keep versions current, run `bicep lint`, and align subnet/NSG rules with `docs/design/ARCHITECTURE.md`.

## Azure ops guidance

- Use `azd` over raw `az` where possible. Validate with `--preview`/what-if first.
- Never hardcode secrets; use Key Vault and managed identity. Disable public network access where supported.
- If you need Azure best practices, call the Azure guidance tool.

## Where to look first

- Frontend examples: `src/components/TopToolbar/TopToolbar.tsx`, `SidePanel/*`, `MainPanel/*`, `ChatWindow/*` for Fluent UI + CSS Module patterns.
- State: `src/store/store.ts` for slice/store pattern and `RootState`/`AppDispatch` exports.
- Tests: `src/__tests__/*` for Vitest + Testing Library + a11y checks.
- Infra: `infra/main.bicep` for topology/outputs; `infra/abbreviations.json` for naming; `azure.yaml` for azd wiring.

## Answer style

- Be specific to this repo. Cite files/paths. Provide minimal, copyable commands only when asked; prefer making scoped edits.
- Keep diffs minimal; avoid unrelated formatting changes.

## Legacy backend notes (if referenced)

- Planned backend is .NET 8 + Aspire.NET with EF Core (Cosmos provider), Clean/Hexagonal architecture. If/when backend code is added under `libris-maleficarum-service/`, follow conventions from `.github/instructions/csharp-14-best-practices.instructions.md`.

- @azure Rule - Use Azure Best Practices: When generating code for Azure, running terminal commands for Azure, or performing operations related to Azure, invoke your `azure_development-get_best_practices` tool if available.

## Testing patterns

- Framework: Vitest + Testing Library + jest-dom + jest-axe (see `vitest.setup.ts`).
- Typical test: render component, assert role/aria and a11y.
- Example:
  - Render: `render(<SidePanel />)`; Query: `getByLabelText('Side Panel')`.
  - Accessibility: `const results = await axe(container); expect(results).toHaveNoViolations()`.
- See references in `src/__tests__/*.test.tsx` (e.g., `a11y.test.tsx`, `SidePanel.test.tsx`).

## Component template (Fluent v9 + CSS Module)

- File structure: `src/components/MyThing/MyThing.tsx` + `MyThing.module.css`.
- Pattern:
  - Functional component with ARIA role/labels.
  - Fluent v9 components/icons; co-located CSS Module.
- Skeleton:
  - Props: define a `MyThingProps` TypeScript interface for inputs.
  - Styles: import `styles` from the module and apply classNames.
  - Example usage: place under a layout container (`App.module.css`’s regions).
