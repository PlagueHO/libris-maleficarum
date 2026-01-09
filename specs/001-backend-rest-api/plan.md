# Implementation Plan: Backend REST API with Cosmos DB Integration

**Branch**: `001-backend-rest-api` | **Date**: 2026-01-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-backend-rest-api/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a .NET 10 ASP.NET Core Web API with Clean Architecture that provides RESTful endpoints for managing worlds, entities, and assets in a world-building application. The API uses Entity Framework Core with Cosmos DB provider for data persistence, implements repository pattern with interfaces in Domain layer, and leverages Aspire.NET AppHost for local development orchestration with Cosmos DB Emulator. Priority 1 features (World Management API + Aspire integration) deliver MVP functionality enabling frontend world creation and retrieval with a single-command developer experience.

## Technical Context

**Language/Version**: .NET 10 with C# 14
**Primary Dependencies**: ASP.NET Core Web API 10.0, Aspire.NET 13.1, Entity Framework Core 10 with Cosmos DB provider, Fluent Validation, Microsoft Agent Framework (future), Azure SDK for .NET  
**Storage**: Azure Cosmos DB (production), Cosmos DB Emulator (local development via Aspire)  
**Testing**: MSTest.Sdk, FluentAssertions, NSubstitute, Microsoft.AspNetCore.Mvc.Testing (integration tests)  
**Target Platform**: Azure Container Apps (production), Aspire Dashboard (local development)
**Project Type**: Multi-project solution - Web API + Domain + Infrastructure + Orchestration  
**Performance Goals**: <200ms p95 response time for CRUD operations, 100 worlds created/retrieved in <5 seconds  
**Constraints**: Cosmos DB 2MB item limit, 10MB asset size default (configurable to 25MB), max 200 items per page pagination  
**Scale/Scope**: Initial: Single-user local development, Production: Multi-tenant with row-level security (OwnerId filtering), 10k+ entities per world

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Cloud-Native Architecture ✅ PASS

- **Compliant**: Infrastructure designed for Azure Container Apps (production) with Aspire for local development
- **Compliant**: Network isolation planned via private endpoints (deferred to infrastructure implementation)
- **Compliant**: All resources will be tagged with `azd-env-name` per existing Bicep configuration

### Clean Architecture & Separation of Concerns ✅ PASS

- **Compliant**: Solution follows Clean/Hexagonal Architecture with Api, Domain, Infrastructure layers
- **Compliant**: Repository pattern with interfaces in Domain, implementations in Infrastructure
- **Compliant**: Dependencies flow inward: Api → Domain ← Infrastructure

#### Architecture Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│                        HTTP Requests                            │
└──────────────────────────┬──────────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                      API Layer (src/Api)                        │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │Controllers │  │ DTOs/Models │  │ Validators/Middleware  │  │
│  └────────────┘  └─────────────┘  └─────────────────────────┘  │
└──────────────────────────┬──────────────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────────────┐
│                   Domain Layer (src/Domain)                     │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Entities  │  │  Interfaces │  │  Exceptions/ValueObjects│  │
│  │ (World, WE)│  │(IRepository)│  │                         │  │
│  └────────────┘  └─────────────┘  └─────────────────────────┘  │
└──────────────────────────▲──────────────────────────────────────┘
                           │ implements
┌──────────────────────────┴──────────────────────────────────────┐
│              Infrastructure Layer (src/Infrastructure)          │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │Repositories│  │  DbContext  │  │ EF Configurations      │  │
│  │   (EF)     │  │   (Cosmos)  │  │ Services               │  │
│  └────────────┘  └─────────────┘  └─────────────────────────┘  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                  ┌────────▼────────┐
                  │  Azure Cosmos DB │
                  │  (via EF Core)   │
                  └──────────────────┘
```

**Dependency Flow**: Api → Domain ← Infrastructure (Clean Architecture principle)
**Key Benefit**: Domain layer has zero external dependencies, enabling unit testing without infrastructure

### Test-Driven Development ✅ PASS

- **Compliant**: Unit tests required for repositories, services, and domain logic in `tests/` projects
- **Compliant**: Integration tests required for API endpoints using AAA pattern and FluentAssertions
- **Compliant**: Test projects: Api.Tests, Domain.Tests, Infrastructure.Tests already exist

### Framework & Technology Standards ✅ PASS

- **Compliant**: Backend uses .NET 10 with Aspire.NET for orchestration
- **Compliant**: EF Core with Cosmos DB provider for data access
- **Compliant**: Microsoft Agent Framework noted for future implementation (not in this feature scope)

### Developer Experience & Inner Loop ✅ PASS

- **Compliant**: Aspire AppHost provides single-command startup (`dotnet run --project src/Orchestration/AppHost`)
- **Compliant**: Aspire Dashboard available for observability (logs, traces, metrics)
- **Compliant**: Connection strings automated via Aspire service discovery

### Security & Privacy by Default ✅ PASS

- **Compliant**: Stubbed IUserContextService abstracts user authentication (production uses Azure Entra ID)
- **Compliant**: Row-level security enforced via OwnerId checks in repositories
- **Compliant**: No hardcoded secrets - Aspire manages local development connection strings

### Semantic Versioning & Breaking Changes ✅ PASS

- **Compliant**: API versioning via URL path `/api/v1/` supports future breaking changes
- **Compliant**: Follows existing GitVersion configuration in repository

**GATE RESULT**: ✅ **PASSED** - All constitutional principles satisfied or planned for future phases

## Project Structure

### Documentation (this feature)

```text
specs/001-backend-rest-api/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (in progress)
├── research.md          # Phase 0: Technology decisions and best practices
├── data-model.md        # Phase 1: Entity schemas and relationships
├── quickstart.md        # Phase 1: Developer quick start guide
├── contracts/           # Phase 1: API contracts (OpenAPI/Swagger specs)
│   ├── worlds.yaml      # World Management endpoints
│   ├── entities.yaml    # Entity Management endpoints
│   └── common.yaml      # Shared schemas and error responses
├── checklists/
│   └── requirements.md  # Requirements validation (completed)
└── tasks.md             # Phase 2: Implementation tasks (future - /speckit.tasks)
```

### Source Code (repository root)

```text
libris-maleficarum-service/
├── LibrisMaleficarum.slnx                    # Solution file
├── src/
│   ├── Api/                                  # HTTP API Layer (ASP.NET Core Web API)
│   │   ├── LibrisMaleficarum.Api.csproj
│   │   ├── Program.cs                        # Entry point with service registration
│   │   ├── appsettings.json                  # Configuration
│   │   ├── appsettings.Development.json
│   │   ├── Controllers/                      # API Controllers
│   │   │   ├── WorldsController.cs           # World Management endpoints
│   │   │   ├── WorldEntitiesController.cs         # Entity Management endpoints
│   │   │   └── AssetsController.cs           # Asset Management endpoints (P3)
│   │   ├── Endpoints/                        # Minimal API endpoints (alternative approach)
│   │   ├── Models/                           # Request/Response DTOs
│   │   │   ├── Requests/                     # API request models
│   │   │   │   ├── CreateWorldRequest.cs
│   │   │   │   ├── UpdateWorldRequest.cs
│   │   │   │   ├── CreateEntityRequest.cs
│   │   │   │   └── UpdateEntityRequest.cs
│   │   │   └── Responses/                    # API response models
│   │   │       ├── WorldResponse.cs
│   │   │       ├── EntityResponse.cs
│   │   │       ├── ApiResponse.cs            # Generic wrapper
│   │   │       └── ErrorResponse.cs
│   │   ├── Middleware/                       # Custom middleware
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Filters/                          # Action filters
│   │   │   └── ValidationFilter.cs
│   │   ├── Extensions/                       # Service registration extensions
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── WebApplicationExtensions.cs
│   │   └── Validators/                       # FluentValidation validators
│   │       ├── CreateWorldRequestValidator.cs
│   │       └── CreateEntityRequestValidator.cs
│   │
│   ├── Domain/                               # Domain Layer (Business Logic)
│   │   ├── LibrisMaleficarum.Domain.csproj
│   │   ├── Entities/                         # Domain entities
│   │   │   ├── World.cs                      # World aggregate root
│   │   │   ├── WorldEntity.cs                # Generic entity in world
│   │   │   ├── Asset.cs                      # Media asset entity
│   │   │   └── DeletedWorldEntity.cs         # Soft-deleted entity
│   │   ├── ValueObjects/                     # Immutable value objects
│   │   │   ├── EntityType.cs                 # Enum: Continent, Country, Character, etc.
│   │   │   └── Tag.cs                        # Entity tag value object
│   │   ├── Interfaces/                       # Repository and service contracts
│   │   │   ├── Repositories/
│   │   │   │   ├── IWorldRepository.cs
│   │   │   │   ├── IWorldEntityRepository.cs
│   │   │   │   └── IAssetRepository.cs
│   │   │   └── Services/
│   │   │       ├── IUserContextService.cs    # User authentication abstraction
│   │   │       └── ISearchService.cs         # Search abstraction (future Azure AI Search)
│   │   ├── Exceptions/                       # Domain-specific exceptions
│   │   │   ├── WorldNotFoundException.cs
│   │   │   ├── UnauthorizedWorldAccessException.cs
│   │   │   └── ValidationException.cs
│   │   └── Events/                           # Domain events (future)
│   │
│   ├── Infrastructure/                       # Infrastructure Layer (External Concerns)
│   │   ├── LibrisMaleficarum.Infrastructure.csproj
│   │   ├── Persistence/                      # EF Core DbContext and configurations
│   │   │   ├── ApplicationDbContext.cs       # Main DbContext
│   │   │   ├── Configurations/               # Entity type configurations
│   │   │   │   ├── WorldConfiguration.cs
│   │   │   │   ├── WorldEntityConfiguration.cs
│   │   │   │   └── AssetConfiguration.cs
│   │   │   └── Migrations/                   # Not used with Cosmos DB
│   │   ├── Repositories/                     # Repository implementations
│   │   │   ├── WorldRepository.cs            # IWorldRepository implementation
│   │   │   ├── WorldEntityRepository.cs      # IWorldEntityRepository implementation
│   │   │   └── AssetRepository.cs            # IAssetRepository implementation
│   │   └── Services/                         # Service implementations
│   │       ├── UserContextService.cs         # Stubbed user context
│   │       └── SearchService.cs              # Basic search implementation
│   │
│   └── Orchestration/                        # Aspire Orchestration
│       ├── AppHost/                          # Aspire AppHost project
│       │   ├── LibrisMaleficarum.AppHost.csproj
│       │   ├── AppHost.cs                    # Aspire application model definition
│       │   ├── appsettings.json
│       │   └── appsettings.Development.json
│       └── ServiceDefaults/                  # Shared service configuration
│           ├── LibrisMaleficarum.ServiceDefaults.csproj
│           └── Extensions.cs                 # AddServiceDefaults() extension
│
└── tests/                                    # Test Projects
    ├── Api.Tests/                            # API Integration Tests
    │   ├── LibrisMaleficarum.Api.Tests.csproj
    │   ├── Controllers/
    │   │   ├── WorldsControllerTests.cs      # World CRUD endpoint tests
    │   │   └── WorldEntitiesControllerTests.cs    # Entity CRUD endpoint tests
    │   ├── Middleware/
    │   │   └── ExceptionHandlingMiddlewareTests.cs
    │   └── Fixtures/
    │       └── WebApplicationFactory.cs      # Test server factory
    │
    ├── Domain.Tests/                         # Domain Unit Tests
    │   ├── LibrisMaleficarum.Domain.Tests.csproj
    │   ├── Entities/
    │   │   ├── WorldTests.cs                 # World entity validation tests
    │   │   └── WorldEntityTests.cs           # Entity validation tests
    │   └── ValueObjects/
    │       └── EntityTypeTests.cs
    │
    └── Infrastructure.Tests/                 # Infrastructure Unit Tests
        ├── LibrisMaleficarum.Infrastructure.Tests.csproj
        ├── Repositories/
        │   ├── WorldRepositoryTests.cs       # Repository tests with in-memory provider
        │   └── WorldEntityRepositoryTests.cs
        └── Services/
            └── SearchServiceTests.cs
```

**Structure Decision**: The solution follows Clean/Hexagonal Architecture with four primary projects in `src/`:

1. **Api** - HTTP layer with controllers, request/response models, middleware, and FluentValidation
1. **Domain** - Business logic with entities, value objects, repository interfaces, domain exceptions
1. **Infrastructure** - External concerns with EF Core repositories, DbContext configurations, service implementations
1. **Orchestration/AppHost** - Aspire.NET orchestration for local development with Cosmos DB Emulator
1. **Orchestration/ServiceDefaults** - Shared configuration for telemetry, health checks, resilience

Dependencies flow: Api → Domain ← Infrastructure. Domain has no external dependencies. Three corresponding test projects use MSTest.Sdk (with Microsoft.Testing.Platform), FluentAssertions, NSubstitute, and AAA pattern.

**Microsoft Agent Framework Note**: While the constitution mandates Microsoft Agent Framework for AI interactions, this backend REST API feature does NOT implement generative AI capabilities. Agent Framework integration is reserved for future AI-powered features (e.g., content generation, semantic search, NPC dialogue). Current implementation focuses on CRUD operations, hierarchical data management, and asset storage per specification.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitutional violations detected. All complexity is justified:

- **Four Projects**: Required by Clean Architecture principle (Api, Domain, Infrastructure, Orchestration)
- **Repository Pattern**: Required by FR-007 and Clean Architecture separation of concerns
- **Multiple Test Projects**: Required by TDD principle III for layer-specific testing

All patterns align with constitutional requirements and follow YAGNI/Clean Code principles.
