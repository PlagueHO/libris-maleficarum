# Agent Development Guide

Libris Maleficarum — AI-enhanced TTRPG campaign management platform.

**Stack**: React 19 + TypeScript + Vite + Redux Toolkit + TailwindCSS + Shadcn/ui (frontend) | .NET 10 + ASP.NET Core + EF Core + Cosmos DB + Aspire.NET (backend) | Azure + Bicep (infra)

## Commands

### Frontend (libris-maleficarum-app/)

```bash
pnpm dev  # Dev server (https://127.0.0.1:4000)
pnpm build  # Build (tsc + vite)
pnpm lint  # Lint
pnpm test  # Test (headless)
pnpm test src/components/shared/TagInput/TagInput.test.tsx  # Single test file
pnpm test -- --grep "accessibility"  # Test pattern match
```

### Backend (libris-maleficarum-service/)

```bash
dotnet run --project src/Orchestration/AppHost  # Start Aspire
dotnet build LibrisMaleficarum.slnx  # Build
dotnet test LibrisMaleficarum.slnx  # All tests
dotnet test --filter TestCategory!=Integration  # Unit tests only
dotnet test --filter FullyQualifiedName~CreateWorld_ValidRequest_ReturnsCreated  # Single test
```

## TypeScript/React Style

**Naming**: Components/Types=`PascalCase`, vars/funcs=`camelCase`, constants=`ALL_CAPS`, files=match component name
**Imports**: Use `@/*` path alias; group React→third-party→project→styles; no unused imports
**Types**: interfaces for objects, types for unions; avoid `any`, use `unknown`; define `ComponentNameProps`
**State**: Redux Toolkit in `src/store/store.ts`; `import type { RootState, AppDispatch }`; use `createSlice`
**Components**: Co-locate tests; JSDoc module comment; export named functions
**Error Handling**: try/catch with async/await; guard early; user-friendly messages

## C#/.NET Style

**Naming**: Classes/Methods/Props=`PascalCase`, params/vars=`camelCase`, private fields=`_camelCase`
**Organization**: File-scoped namespaces (`namespace LibrisMaleficarum.Api;`); one type per file; usings ordered System→third-party→project
**Modern C#**: Primary constructors; collection expressions `[1, 2, 3]`; nullable types with `?.`, `??`; switch expressions
**Async**: `Task<T>`/`ValueTask<T>`; accept `CancellationToken`; `ConfigureAwait(false)` in libraries
**Style**: Opening braces on new lines; `var` when obvious; explicit access modifiers; `readonly`/`required`

## Testing

**Frontend** (Vitest + Testing Library + jest-axe):

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { store } from '@/store/store';

expect.extend(toHaveNoViolations);
describe('Component', () => {
  it('renders and has no a11y violations', async () => {
    const { container } = render(<Provider store={store}><Component /></Provider>);
    expect(screen.getByLabelText('Label')).toBeInTheDocument();
    expect(await axe(container)).toHaveNoViolations();
  });
});
```

**Backend** (MSTest + FluentAssertions):

```csharp
[TestClass]
public class ControllerTests {
    [TestMethod]
    public async Task Method_Scenario_ExpectedResult() {
        var result = await controller.MethodAsync(request);
        result.Should().BeOfType<CreatedResult>();
    }
}
// Use [TestCategory("Integration")] and [TestCategory("RequiresDocker")] for integration tests
```

## Key Patterns

**Accessibility**: WCAG 2.2 Level AA; semantic HTML; ARIA labels; jest-axe in all tests; keyboard nav; contrast ratios 4.5:1/3:1
**Styling**: TailwindCSS only; use `cn()` utility from `@/lib/utils`; Shadcn/ui components (Dialog, Select, Button, Input, ScrollArea, etc.); responsive with `sm:`, `md:`, `lg:` breakpoints
**API Client**: Axios + axios-retry; env config; MSW for mocking
**Data Model**: WorldEntity hierarchy; partition key `/WorldId`; GUID IDs; soft delete with `IsDeleted`
**Clean Arch**: Api (controllers/DTOs) → Domain (entities/interfaces) ← Infrastructure (EF/repos)

## Critical Rules

- **UI Framework**: Use **Shadcn/ui + Radix UI** (NOT Fluent UI)
- **Styling**: **TailwindCSS only** (NOT CSS Modules)
- **Azure**: Never hardcode secrets (use Key Vault + managed identity); private endpoints; prefer `azd` over `az`
- **AI**: Microsoft Agent Framework (not Semantic Kernel) for backend; CopilotKit for frontend
- **Aspire.NET**: Local dev only (`dotnet run --project src/Orchestration/AppHost`)
- **Logging (C# only)**: Never log raw user-derived values (query/transcription text, access codes, display names, user-authored content). Log safe metadata only: counts, lengths, durations, GUIDs, HTTP status codes, lifecycle event names. See `docs/design/security-logging.md` and `.github/instructions/logging-security.instructions.md`.

## Agent guardrails

### CI/CD workflow contract

- **Caller owns token permissions** — reusable workflows never grant themselves scope beyond what the caller job declares.
- **CI never needs `packages: write`** — build and test only; image push belongs in a dedicated publish workflow.
- **Workflow file names must match responsibility** — `build-*.yml` builds/tests only; `publish-*.yml` pushes artifacts. Rename if purpose changes.
- **On any workflow file change**: run a quick permissions audit (`workflow_call:` files should have no `packages: write`).

### Command execution protocol

- Execute the user's stated command exactly; no substitutions without asking first.
- After running, always emit `Command: <exact command>` + `Result: <concise outcome>` — even for read-only queries.
- If a user turn is read-only ("check", "report", "inspect"), do not edit files.
- If output is missing/ambiguous, rerun once with explicit capture (`2>&1`) and explain the gap.

### Build / lint / test triage

When a build, lint, or test fails:

1. Reproduce with the exact failing command; collect the exact error.
1. Apply the **smallest** fix that addresses the root cause.
1. Re-run **only** the affected task to verify — not the full suite.
1. Report: root cause + fix applied + verification command output.

- Do **not** apply multiple speculative fixes before re-testing.

### Cross-repo pattern parity

When mirroring a pattern from another repo (e.g., `prompt-babbler`, `marginalia`):

- Copy names verbatim; change **only** the segment that legitimately differs.
- Include a one-line parity note: what was copied unchanged and what was intentionally different.
- Before marking done, verify the changed files against the source repo structure.

### Context growth

- When a session exceeds ~15 turns or spans multiple distinct work phases, proactively suggest `/compact` or starting a fresh thread.
- Provide a handoff summary on compaction: current state, files changed, next steps.

## Post-Generation

- Trim trailing whitespace
- Remove unused imports
- Indent: 2 spaces (TS/React), 4 spaces (C#)
- Run lint/format
- Update tests

## Docs

See `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md`, `docs/design/*.md` for details.
