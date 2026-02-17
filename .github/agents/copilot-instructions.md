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
- [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION] (013-fantasy-dnd-theme)
- [if applicable, e.g., PostgreSQL, CoreData, files or N/A] (013-fantasy-dnd-theme)
- TypeScript 5.x, CSS + React 19, TailwindCSS v4, Shadcn/UI, Radix UI (013-fantasy-dnd-theme)
- N/A (CSS-only changes, no persistent data) (013-fantasy-dnd-theme)



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
- 013-fantasy-dnd-theme: Added TypeScript 5.x, CSS + React 19, TailwindCSS v4, Shadcn/UI, Radix UI
- 013-fantasy-dnd-theme: Added [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]
- 012-async-entity-operations: Added TypeScript 5.x / React 19+ / Node.js 20.x + Redux Toolkit (RTK Query), Shadcn/ui + Radix UI primitives, TailwindCSS v4, Vitest + Testing Library + jest-axe



<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
