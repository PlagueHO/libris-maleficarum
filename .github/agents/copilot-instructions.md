# libris-maleficarum Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-01-21

## Active Technologies
- TypeScript 5.x (existing) + React 19, Redux Toolkit, Vitest, jest-axe (all existing) (007-entity-type-registry)
- N/A (refactor of in-memory constants) (007-entity-type-registry)
- TypeScript 5.x + React 19 + Redux Toolkit, React Router, Shadcn/ui + Radix UI, TailwindCSS v4, Vitest + Testing Library, RTK Query (008-edit-world-entity)
- Azure Cosmos DB (via REST API), client-side Redux state (008-edit-world-entity)
- .NET 10, C# 14 + EF Core 10 (Cosmos DB provider), ASP.NET Core Minimal APIs/Controllers, Aspire.NET, OpenTelemetry (011-soft-delete-entities)
- Azure Cosmos DB (WorldEntity container with partition key `[/WorldId, /id]`) (011-soft-delete-entities)
- .NET 10, C# 14 + ASP.NET Core, EF Core 10 (Cosmos DB provider), Aspire.NET (local dev) (011-soft-delete-entities)
- Azure Cosmos DB (WorldEntity container with hierarchical partition key `[/WorldId, /id]`) (011-soft-delete-entities)
- TypeScript 5.x / React 19+ / Node.js 20.x + Redux Toolkit (RTK Query), Shadcn/ui + Radix UI primitives, TailwindCSS v4, Vitest + Testing Library + jest-axe (012-async-entity-operations)
- Session-only (in-memory Redux state), no persistence across browser sessions (012-async-entity-operations)
- TypeScript 5.x, React 19, ES2022 target + Vite 7.x, TailwindCSS v4, Shadcn/UI (Radix primitives), Lucide React icons, Redux Toolkit (013-fantasy-dnd-theme)
- localStorage (theme preference key `theme`), no backend changes (013-fantasy-dnd-theme)
- TypeScript 5.x (React 19+ frontend) + C# 14 (.NET 10 backend) + React 19, Redux Toolkit, lucide-react, Vitest, Testing Library, jest-axe (frontend) | EF Core Cosmos DB, FluentValidation, Aspire.NET (backend) (014-player-character-entity)
- Azure Cosmos DB (partition key `/WorldId`, `Attributes` stored as JSON string) (014-player-character-entity)
- C# / .NET 10 (ASP.NET Core, EF Core with Cosmos DB provider) + Azure.Search.Documents (AI Search SDK), Azure.AI.OpenAI (embedding generation), Microsoft.Extensions.AI (abstraction layer), Aspire.Hosting.Azure.Search (AppHost), OpenTelemetry (015-entity-search-index)
- Azure Cosmos DB (WorldEntity container, hierarchical partition key `[/WorldId, /id]`) → Azure AI Search (vector + text index) (015-entity-search-index)
- C# 14 / .NET 10 (aligned with `global.json` SDK 10.0.100) + `System.CommandLine` (CLI parsing), `Microsoft.Extensions.Http.Resilience` (HTTP retry/resilience), `System.Text.Json` (JSON parsing), `System.IO.Compression` (zip handling) (016-world-data-importer)
- N/A (reads local files, writes to backend API via HTTP) (016-world-data-importer)
- TypeScript 5.9 (frontend), C# / .NET 10 (backend) (017-user-auth-menu)
- Azure Cosmos DB (World container `/Id`, WorldEntity container `[/WorldId, /id]`, Asset container `[/WorldId, /EntityId]`) (017-user-auth-menu)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for 

## Code Style

General: Follow standard conventions

## Recent Changes
- 017-user-auth-menu: Added TypeScript 5.9 (frontend), C# / .NET 10 (backend)
- 016-world-data-importer: Added C# 14 / .NET 10 (aligned with `global.json` SDK 10.0.100) + `System.CommandLine` (CLI parsing), `Microsoft.Extensions.Http.Resilience` (HTTP retry/resilience), `System.Text.Json` (JSON parsing), `System.IO.Compression` (zip handling)
- 015-entity-search-index: Added C# / .NET 10 (ASP.NET Core, EF Core with Cosmos DB provider) + Azure.Search.Documents (AI Search SDK), Azure.AI.OpenAI (embedding generation), Microsoft.Extensions.AI (abstraction layer), Aspire.Hosting.Azure.Search (AppHost), OpenTelemetry

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
