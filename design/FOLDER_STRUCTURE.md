# Folder Structure

The repository is organized for clarity, scalability, and best practices for both .NET 8 backend and React/TypeScript frontend development.

```text
/
├── .devcontainer/           # GitHub Codespaces/dev container config
├── .github/                 # GitHub Actions workflows and community files
├── infra/                   # Infrastructure as Code (Bicep templates, deployment scripts)
├── backend/                 # .NET 8 backend solution
│   ├── src/                         # All backend projects and solution
│   │   ├── LibrisMaleficarum.sln            # Solution file
│   │   ├── Api/                            # ASP.NET Core minimal API project
│   │   │   └── LibrisMaleficarum.Api.csproj
│   │   ├── Application/                    # Application layer (CQRS, services, DTOs)
│   │   │   └── LibrisMaleficarum.Application.csproj
│   │   ├── Domain/                         # Domain entities, value objects, interfaces
│   │   │   └── LibrisMaleficarum.Domain.csproj
│   │   ├── Infrastructure/                 # EF Core, Cosmos DB, repository implementations
│   │   │   └── LibrisMaleficarum.Infrastructure.csproj
│   │   └── tests/                          # Backend unit/integration tests
│   │       ├── LibrisMaleficarum.Api.Tests/
│   │       ├── LibrisMaleficarum.Application.Tests/
│   │       ├── LibrisMaleficarum.Domain.Tests/
│   │       └── LibrisMaleficarum.Infrastructure.Tests/
├── frontend/                # React + TypeScript frontend app
│   ├── src/                 # Source code (components, hooks, services, types, etc.)
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── services/
│   │   ├── types/
│   │   ├── App.tsx
│   │   └── index.tsx
│   ├── public/              # Static assets (index.html, images, etc.)
│   ├── tests/               # Frontend unit/integration tests
│   ├── package.json
│   ├── tsconfig.json
│   └── ...                  # Other config files (vite.config.ts, .env, etc.)
├── README.md                # Project overview and getting started
├── DESIGN.md                # Architecture and design documentation
└── ...                      # Solution-level files, .editorconfig, etc.
```

**Key Points:**
- Backend code is under `backend/src/` following Clean/Hexagonal Architecture.
- Frontend code is isolated in `frontend/`.
- Infrastructure as Code is in `infra/`.
- Codespaces/devcontainer config is in `.devcontainer/`.
- CI/CD and GitHub workflows are in `.github/`.
- Tests are separated in `backend/src/tests/` and `frontend/tests/`.
