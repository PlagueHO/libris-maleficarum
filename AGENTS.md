# Libris Maleficarum - Agent Instructions

See `.github/copilot-instructions.md` for code patterns.

## Commands

Setup, Markdown checks, and application startup, from the repository root:

```powershell
pnpm install
# Repository-wide lint applies automatic fixes; review the resulting diff.
pnpm lint:md:ci
aspire run
```

Frontend checks, from `libris-maleficarum-app/`:

```powershell
pnpm install
pnpm lint
pnpm test
pnpm build
# Single test file
pnpm test src/components/shared/TagInput/TagInput.test.tsx
```

Backend checks, from `libris-maleficarum-service/`:

```powershell
dotnet restore LibrisMaleficarum.slnx
dotnet build LibrisMaleficarum.slnx
dotnet format LibrisMaleficarum.slnx --verify-no-changes --severity error
dotnet test --solution LibrisMaleficarum.slnx --filter TestCategory=Unit
```

- Use Node.js 22 and pnpm 10 or later.
- Use the SDK in `libris-maleficarum-service/global.json`.
- Use Aspire MCP tools to select the AppHost and open the `frontend` resource.

## Layout

```text
libris-maleficarum-app/       React frontend; src/ includes co-located tests
libris-maleficarum-service/   src/: Api, Domain, Infrastructure, Orchestration
                            tests/: unit, integration, performance
infra/                      Azure Bicep
docs/                       VitePress documentation and design contracts
registries/, schemas/       Entity type registries and schemas
samples/, tests/smoke/       Sample worlds and smoke tests
.github/workflows/          CI/CD workflows
```

## Change Checklist

- Run the checks for each area you change.
- Add or update tests when behavior changes.
- Read `docs/design/data_model.md` before changing APIs, repositories, persistence, schemas, import/export contracts, frontend entity types, API clients, or form data.
- Match final field names and shapes to that document unless the user directs otherwise.
- Include a review note confirming the data-model check for those changes.
- Ask before adding dependencies, pushing commits, or deleting files.
- For inspection-only requests, do not edit files.
- Ask before substituting a user-specified command.
- Report each executed command and its result.
- Capture ambiguous output once more with stderr included.
- Reproduce a failed check before making one targeted fix.
- Rerun only the affected check after the fix.
- Copy pattern names verbatim unless a segment must differ; verify source parity and explain each difference.

## CI Pipeline

- **Markdown lint** fails on Markdown formatting errors.
- **Frontend build** checks generated registries, ESLint, Vitest, and the
  production build.
- **Backend build** checks compilation, formatting, unsafe log patterns, and
  unit tests.
- **Infrastructure validation** runs Bicep validation and lint checks.
- **Docs build** checks the VitePress build in `docs/`.

## Conventions

| Concern | Rule |
|---|---|
| Pull requests | Use `.github/PULL_REQUEST_TEMPLATE.md`. |
| Workflow permissions | The caller owns token permissions; reusable workflows cannot grant more scope. |
| Build workflows | Build and test only; do not grant `packages: write`. |
| Publish workflows | Keep image publication separate from build workflows. |
| Workflow edits | Audit reusable workflow permissions after each change. |

## Documentation and change records

- Review documentation and release notes for each change.
- Record notable changes in the `[Unreleased]` section of `CHANGELOG.md`.
- Use the existing changelog format and section names.
- Include required migration steps for breaking changes.
- Update `README.md` and affected files in `docs/` when behavior or requirements change.
- Update affected examples, setup commands, and configuration guidance.
- Keep pull request release notes consistent with `CHANGELOG.md`.
- Do not rewrite released changelog entries.
- Change release versions only when the task requires a release.
