# Libris Maleficarum - Backend Service

**World-building management system for tabletop RPG campaigns**

This repository contains the backend REST API service built with .NET 10, ASP.NET Core, Entity Framework Core (Cosmos DB provider), and Aspire.NET for local development orchestration.

## Quick Start

Get up and running in under 5 minutes with Aspire's single-command developer experience.

### Prerequisites

- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop**: Required for Cosmos DB Emulator
- **Visual Studio Code** or **Visual Studio 2022** (17.12+)

### Getting Started

1. **Clone and navigate to service directory**:

   ```powershell
   git clone https://github.com/PlagueHO/libris-maleficarum.git
   cd libris-maleficarum/libris-maleficarum-service
   ```

2. **Restore dependencies**:

   ```powershell
   dotnet restore LibrisMaleficarum.slnx
   ```

3. **Start all services with Aspire**:

   ```powershell
   dotnet run --project src/Orchestration/AppHost
   ```

   This single command:
   - ✅ Starts Cosmos DB Emulator in Docker
   - ✅ Starts the API service on HTTPS
   - ✅ Opens Aspire Dashboard at https://localhost:15888
   - ✅ Configures service discovery and connections automatically

4. **Verify services are running**:

   Open the Aspire Dashboard: **https://localhost:15888**

   All services should show green status:
   - `cosmosdb` - Cosmos DB Emulator
   - `api` - REST API service

5. **Test the API**:

   Navigate to Swagger UI: **https://localhost:7041/swagger**

   Try creating your first world:

   ```json
   POST /api/v1/worlds
   {
     "name": "My Campaign World",
     "description": "An epic fantasy setting"
   }
   ```

📖 **For detailed instructions**, see [Quick Start Guide](../specs/001-backend-rest-api/quickstart.md)

## Architecture

This service follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│         API Layer (Controllers)         │  ← HTTP Requests/Responses
├─────────────────────────────────────────┤
│       Domain Layer (Entities)           │  ← Business Logic & Rules
├─────────────────────────────────────────┤
│   Infrastructure (Repositories, EF)     │  ← Data Access & External Services
└─────────────────────────────────────────┘
              ↓
        Cosmos DB (via EF Core)
```

### Project Structure

- **`src/Api/`** - ASP.NET Core Web API
  - Controllers (WorldsController, EntitiesController, AssetsController)
  - DTOs (Request/Response models)
  - Validators (FluentValidation)
  - Middleware (Exception handling)

- **`src/Domain/`** - Core business logic
  - Entities (World, WorldEntity, Asset)
  - Repository interfaces
  - Domain exceptions
  - Value objects

- **`src/Infrastructure/`** - Data access & external services
  - EF Core DbContext
  - Repository implementations
  - Cosmos DB configurations
  - Blob storage service

- **`src/Orchestration/AppHost/`** - Aspire orchestration
  - Service definitions
  - Local development configuration

- **`tests/`** - Unit and integration tests
  - Api.Tests (Controller tests)
  - Domain.Tests (Entity validation tests)
  - Infrastructure.Tests (Repository tests)

## Features Implemented

### ✅ Phase 1-6 Complete (MVP Functional)

- **World Management API** (User Story 1)
  - CRUD operations for RPG campaign worlds
  - Cursor-based pagination
  - Soft-delete support
  - ETag-based optimistic concurrency

- **Entity Management API** (User Story 2)
  - Create and manage entities (characters, locations, campaigns, etc.)
  - 15 entity types (Character, Location, Campaign, Session, Faction, Item, Quest, Event, Continent, Country, Region, City, Building, Room, Other)
  - Hierarchical parent-child relationships
  - Cascade delete support
  - Tagging and custom attributes (JSON)

- **Asset Management API** (User Story 3)
  - File upload (images, audio, video, documents)
  - 14 supported content types (jpg/png/gif/webp, mp3/wav/ogg, mp4/webm, pdf/txt/md)
  - 25MB default file size limit (configurable)
  - Azure Blob Storage integration
  - SAS token-based secure downloads

- **Search & Filter API** (User Story 4)
  - Case-insensitive full-text search (Name, Description, Tags)
  - Sorting by Name, CreatedDate, ModifiedDate
  - Offset-based pagination
  - Max 200 results per page

### 🎯 Phase 7 In Progress

- **Aspire Integration** (User Story 5)
  - Single-command startup
  - Cosmos DB Emulator orchestration
  - Service discovery
  - Observability dashboard

## Development

### Running Tests

```powershell
# Run all tests
dotnet test LibrisMaleficarum.slnx

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Domain.Tests/LibrisMaleficarum.Domain.Tests.csproj

# Run tests in watch mode
dotnet watch test --project tests/Api.Tests
```

### Building

```powershell
# Build entire solution
dotnet build LibrisMaleficarum.slnx

# Build in Release mode
dotnet build LibrisMaleficarum.slnx --configuration Release
```

### Code Formatting

```powershell
# Format all code
dotnet format LibrisMaleficarum.slnx

# Verify formatting (CI/CD)
dotnet format LibrisMaleficarum.slnx --verify-no-changes
```

## API Documentation

### Swagger/OpenAPI

Interactive API documentation is available at:

**https://localhost:7041/swagger** (when running locally)

### API Contracts

Detailed OpenAPI specifications are maintained in:

- `specs/001-backend-rest-api/contracts/worlds.yaml` - World Management
- `specs/001-backend-rest-api/contracts/entities.yaml` - Entity Management
- `specs/001-backend-rest-api/contracts/assets.yaml` - Asset Management

## Technology Stack

- **.NET 10** - Runtime and SDK
- **ASP.NET Core** - Web framework
- **Entity Framework Core 10** - ORM with Cosmos DB provider
- **Azure Cosmos DB** - NoSQL database (partition key: WorldId)
- **Azure Blob Storage** - Asset file storage
- **Aspire.NET 13.1** - Local development orchestration
- **FluentValidation** - Request validation
- **MSTest** - Test framework
- **FluentAssertions** - Test assertion library
- **NSubstitute** - Mocking library

## Observability

Aspire provides built-in observability via the dashboard:

- **Logs** - Real-time application logs with filtering
- **Traces** - Distributed tracing across services
- **Metrics** - Performance counters and health metrics
- **Health Checks** - Service health status monitoring

Access dashboard: **https://localhost:15888**

## Troubleshooting

### Cosmos DB Emulator Won't Start

Ensure Docker Desktop is running:

```powershell
docker ps
```

If needed, restart Aspire AppHost.

### API Returns 503

Cosmos DB may still be initializing (first run takes ~2 minutes). Check Aspire Dashboard for service status.

### Tests Fail

Ensure no AppHost is running:

```powershell
# Stop any running Aspire instances
$proc = Get-Process | Where-Object { $_.ProcessName -like "*AppHost*" }
if ($proc) { Stop-Process -Id $proc.Id -Force }
```

## Contributing

See main repository [CONTRIBUTING.md](../CONTRIBUTING.md) for development guidelines.

## License

See main repository [LICENSE](../LICENSE) for license information.

## Documentation

- [Quick Start Guide](../specs/001-backend-rest-api/quickstart.md) - Detailed getting started
- [Data Model](../specs/001-backend-rest-api/data-model.md) - Entity schemas and relationships
- [Technical Decisions](../specs/001-backend-rest-api/research.md) - Architecture and technology choices
- [Task Breakdown](../specs/001-backend-rest-api/tasks.md) - Implementation phases

---

**Status**: Phase 6 Complete ✅ | Phase 7 In Progress 🎯

Built with ❤️ for tabletop RPG enthusiasts
