# Copilot instructions for this repo

Use these focused rules to be productive quickly in this monorepo. Keep answers concise and actionable.

## Architecture snapshot

- Frontend: React + TypeScript app in `libris-maleficarum-app/` (Vite build, Redux Toolkit state). Entry: `src/main.tsx`, `src/App.tsx`. Store: `src/store/store.ts`.
  - UI: Fluent UI React components v9 or above (see `src/components/*`). Get Fluent UI docs from `https://react.fluentui.dev/?path=/docs/concepts-introduction--docs`
- Infra: Azure Bicep in `infra/` using Azure Verified Modules (AVM), subscription-scope deploy via `infra/main.bicep` and `main.bicepparam`.
- Docs: Design/architecture under `docs/design/` (see `ARCHITECTURE.md`, `TECHNOLOGY.md`, `FOLDER_STRUCTURE.md`).
- Azure Developer CLI (azd): root `azure.yaml` wires infra + the app service.

## Day-1 workflows

- Frontend dev: in `libris-maleficarum-app/`
  - `pnpm dev` (or `pnpm start`) → launches Vite on https://127.0.0.1:4000
  - `pnpm build` → type-check + build; `pnpm lint`, `pnpm preview`
- Infra provisioning:
  - Update `infra/main.bicepparam` env values (AZURE_ENV_NAME, AZURE_LOCATION, etc.).
  - Use `azd provision --preview` before changes; then `azd up` to deploy.
  - Outputs (e.g., `CONTAINER_APPS_ENVIRONMENT_NAME`) are defined at bottom of `infra/main.bicep`.

## Patterns and conventions (enforced)

- TypeScript/React
  - Functional components + hooks; CSS Modules for styles (see `src/components/*/*.module.css`).
  - Types via interfaces; prefer immutable data (`const`, `readonly`); use `?.` and `??` operators.
  - Redux Toolkit pattern in `src/store/store.ts` (use `createSlice`, export actions/selectors).
- Naming
  - PascalCase for components, interfaces, types. camelCase for vars/functions. Constants ALL_CAPS.
  - Private class fields prefixed with `_` when classes are used.
- Error handling
  - Wrap async with try/catch; log with context. Add React error boundaries where UI risk exists.

## Infra specifics (AVM + Bicep)

- Registry refs are pinned: `br/public:avm/res/{service}/{resource}:{version}`. Keep versions current.
- Network isolation: services use Private Endpoints; Private DNS zones linked to VNet (see `main.bicep`).
- Key parameters: `environmentName`, `location`, optional `resourceGroupName`, `createBastionHost`.
- Tagging: every resource tagged with `azd-env-name`.
- When editing Bicep:
  - Prefer AVM modules; update versions via MCR tags API; run `bicep lint`.
  - Keep subnets/NSGs consistent with `docs/design/ARCHITECTURE.md`.

## Azure ops guidance

- Use `azd` over raw `az` where possible. Validate with `--preview`/what-if first.
- Never hardcode secrets; use Key Vault and managed identity. Disable public network access where supported.
- If you need Azure best practices, call the Azure guidance tool.

## Where to look first

- Frontend examples: `src/components/TopToolbar/TopToolbar.tsx`, `SidePanel/*`, `MainPanel/*` for CSS module + functional component patterns.
- State: `src/store/store.ts` shows the slice pattern and RootState/AppDispatch exports.
- Infra: `infra/main.bicep` for full resource topology and outputs; `infra/abbreviations.json` for name prefixes.
- Repo wiring: `azure.yaml` connects infra path and service definition.

## Answer style

- Be specific to this repo. Cite files/paths. Provide minimal, copyable commands (pwsh) only when asked to run.
- Prefer edits over advice when safe. Keep diffs minimal and avoid unrelated formatting changes.

## Legacy backend notes (if referenced)

- Planned backend is .NET 8 + Aspire.NET with EF Core (Cosmos provider), Clean/Hexagonal architecture. Follow the conventions listed here if backend code is added under `libris-maleficarum-service/`.

- @azure Rule - Use Azure Best Practices: When generating code for Azure, running terminal commands for Azure, or performing operations related to Azure, invoke your `azure_development-get_best_practices` tool if available.
