# Folder Structure

The repository is organized for clarity, scalability, and best practices for both .NET 10 backend services and React/TypeScript frontend development.

> [!NOTE]
> The backend folder structure (`libris-maleficarum-service/`) is documented below for planning purposes but **is not yet implemented**. Only the frontend application (`libris-maleficarum-app/`) currently exists in the repository.

```text
/
├── .devcontainer/           # GitHub Codespaces/dev container config
├── .github/                 # GitHub Actions workflows and community files
├── infra/                   # Infrastructure as Code (Bicep templates, deployment scripts)
├── libris-maleficarum-service/      # .NET 10 backend solution
│   ├── src/                         # All backend projects and solution
│   │   ├── LibrisMaleficarum.sln           # Solution file
│   │   ├── Api/                            # ASP.NET Core minimal API project
│   │   │   ├── LibrisMaleficarum.Api.csproj
│   │   │   ├── Controllers/                # API controllers
│   │   │   ├── Middleware/                 # Custom middleware
│   │   │   ├── Extensions/                 # Extension methods for API setup
│   │   │   └── Program.cs                  # Entry point for the API
│   │   ├── Domain/                         # Domain entities, value objects, interfaces
│   │   │   ├── LibrisMaleficarum.Domain.csproj
│   │   │   ├── Entities/                   # Domain entities
│   │   │   ├── ValueObjects/               # Value objects
│   │   │   ├── Interfaces/                 # Domain interfaces
│   │   │   └── Events/                     # Domain events
│   │   ├── Infrastructure/                 # EF Core, Cosmos DB, repository implementations
│   │   │   ├── LibrisMaleficarum.Infrastructure.csproj
│   │   │   ├── Persistence/                # EF Core DbContext and migrations
│   │   │   ├── Repositories/               # Repository implementations
│   │   │   ├── Configurations/             # Entity configurations for EF Core
│   │   │   └── Services/                   # Infrastructure services (e.g., email, logging)
│   │   ├── Orchestration/                  # Aspire.NET orchestration layer
│   │   │   ├── LibrisMaleficarum.Orchestration.csproj
│   │   │   ├── Workflows/                  # Workflow definitions
│   │   │   ├── Activities/                 # Activity implementations
│   │   │   └── Extensions/                 # Orchestration extensions
│   │   └── Tests/                          # Backend unit/integration tests
│   │       ├── Api.Tests/                  # Tests for API layer
│   │       ├── Domain.Tests/               # Tests for Domain layer
│   │       ├── Infrastructure.Tests/       # Tests for Infrastructure layer
│   │       └── Orchestration.Tests/        # Tests for Orchestration layer
├── libris-maleficarum-app/  # React + TypeScript frontend app
│   ├── src/                 # Source code (components, hooks, services, types, etc.)
│   │   ├── components/      # React components
│   │   ├── hooks/           # Custom React hooks
│   │   ├── services/        # API calls and business logic
│   │   ├── types/           # TypeScript type definitions
│   │   ├── App.tsx          # Main application component
│   │   └── index.tsx        # Entry point for the app
│   ├── public/              # Static assets (index.html, images, etc.)
│   ├── tests/               # Frontend unit/integration tests
│   ├── package.json         # Node.js dependencies
│   ├── tsconfig.json        # TypeScript configuration
│   └── ...                  # Other config files (vite.config.ts, .env, etc.)
├── README.md                # Project overview and getting started
├── DESIGN.md                # Architecture and design documentation
└── ...                      # Solution-level files, .editorconfig, etc.
```

**Key Points:**

- Backend code is under `backend/src/` following Clean/Hexagonal Architecture.
- Frontend code is isolated in `libris-maleficarum-app/`.
- Infrastructure as Code is in `infra/`.
- Codespaces/devcontainer config is in `.devcontainer/`.
- CI/CD and GitHub workflows are in `.github/`.
- Tests are separated in `backend/src/tests/` and `libris-maleficarum-app/tests/`.
