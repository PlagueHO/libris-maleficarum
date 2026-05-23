---
title: Contributing Guide
description: How to contribute code, documentation, and issues to Libris Maleficarum.
---

## Before You Start

Thank you for contributing to Libris Maleficarum.

Use these defaults when setting up your environment:

* Node.js 22 LTS
* pnpm 10+
* .NET 10 SDK
* Docker Desktop

## Local Setup

1. Fork and clone the repository.
1. Install frontend dependencies.
1. Restore backend dependencies.

```powershell
cd libris-maleficarum-app
pnpm install

cd ..\libris-maleficarum-service
dotnet restore LibrisMaleficarum.slnx
```

## Development Workflows

Run the full stack locally with Aspire:

```powershell
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj
```

Run frontend-only development:

```powershell
cd libris-maleficarum-app
pnpm dev
```

## Quality Gates

Before opening a pull request, run the same core checks used in CI:

```powershell
# Frontend
cd libris-maleficarum-app
pnpm lint
pnpm test

# Backend
cd ..\libris-maleficarum-service
dotnet format LibrisMaleficarum.slnx --verify-no-changes
dotnet test --solution LibrisMaleficarum.slnx --filter TestCategory=Unit

# Markdown
cd ..
pnpm lint:md:ci
```

## Pull Requests

When submitting a pull request:

* Keep changes scoped to one problem.
* Include tests or explain why tests are not needed.
* Update documentation when behavior changes.
* Use the pull request template and reference related issues.

## Reporting Bugs and Requesting Features

Use GitHub issue templates for:

* Bug reports
* Feature requests
* Maintenance chores

Include clear reproduction steps and expected behavior.

## Code Style

Follow repository conventions defined in:

* AGENTS.md
* .github/copilot-instructions.md
* .github/instructions/*.instructions.md
