# Copilot Instructions

See `AGENTS.md` for layout and commands.

## Purpose

Libris Maleficarum is a TTRPG world-building application. The frontend uses
React, TypeScript, Vite, Redux Toolkit, and Tailwind CSS. The service uses .NET,
ASP.NET Core, EF Core, and Cosmos DB. Aspire runs the local application.

## Security and Data

- Never hardcode secrets.
- Keep secrets in Key Vault or configured secret storage.
- Use managed identity for Azure service access.
- Never log raw user content, queries, transcriptions, access codes, or display names in C#.
- Log only safe metadata: counts, lengths, durations, GUIDs, status codes, and lifecycle events.
- Do not use log sanitization as permission to log sensitive content.
- Use `System.Text.Json` for C# JSON.
- Do not use Newtonsoft.Json.
- Configure third-party JSON integrations to use `System.Text.Json`.
- Persist document fields as camelCase.
- Keep `properties` and `systemProperties` as nested JSON objects.
- Use private endpoints and disable public access where supported.
- Enable RBAC for Key Vault.

## Code Style

- Use `.editorconfig` for indentation and line endings.
- Define a named props interface for each component.
- Use interfaces for object shapes and types for unions.
- Prefer `unknown` with type narrowing over `any`.
- Use `@/` for application imports.
- Group imports as React, third-party, project, then styles.
- Import TypeScript types with `import type`.
- Use async/await with explicit error handling.
- Show frontend failures through the existing notification pattern.

## Naming

| Element | Convention | Example |
|---|---|---|
| Components and types | PascalCase | `WorldSidebar`, `WorldEntity` |
| Shadcn/UI files | kebab-case | `button.tsx` |
| Variables and functions | camelCase | `resetToHome` |
| Constants | UPPER_SNAKE_CASE | `MAX_RETRIES` |

## Frontend Patterns

- Use functional React components and hooks.
- Use Shadcn/UI with Radix primitives.
- Do not use Fluent UI.
- Use Tailwind CSS for styling.
- Do not add CSS Modules.
- Use `cn()` from `@/lib/utils` to combine conditional classes.
- Use Redux Toolkit and the existing RTK Query API for shared data.
- Import `RootState` and `AppDispatch` from the store when needed.
- Follow `libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx` for component structure.
- Use PascalCase folders and barrel exports for shared components.
- Keep domain components out of the Shadcn/UI primitives folder.

## Backend Patterns

- Use file-scoped namespaces and one primary type per file.
- Use PascalCase for types and members.
- Use `_camelCase` for private fields.
- Accept and pass `CancellationToken` through asynchronous operations.
- Use `Task`, `Task<T>`, or `ValueTask<T>` for asynchronous methods.
- Use `ConfigureAwait(false)` in library code.
- Keep domain entities and interfaces independent of infrastructure.
- Use Microsoft Agent Framework, not Semantic Kernel, for generative AI.
- Use CopilotKit for frontend agent interactions.
- Use pinned Azure Verified Modules for Bicep resources.

## Testing

- Write frontend tests with Vitest and Testing Library.
- Keep frontend tests beside their components or modules.
- Use `userEvent` to test user interactions.
- Assert accessibility with jest-axe in component tests.
- Wrap Redux-connected components with a test store provider.
- Follow `libris-maleficarum-app/src/components/shared/TagInput/TagInput.test.tsx` for interaction tests.
- Write backend tests with MSTest and FluentAssertions.
- Mark backend tests as `Unit` or `Integration`.

## Further Guidance

- Follow matching files in `.github/instructions/` for language, accessibility,
  logging, and infrastructure rules.
