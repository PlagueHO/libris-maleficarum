# Backend Architecture

The Libris Maleficarum backend is a .NET 10 ASP.NET Core Web API hosted in Azure Container Apps. The backend uses Aspire for local development orchestration and follows Clean/Hexagonal Architecture principles.

> [!NOTE]
> The backend implementation is **not yet implemented**. This document describes the planned architecture and design decisions.

## Technology Stack

- **[.NET 10](https://learn.microsoft.com/en-us/dotnet/)** – Latest LTS version of .NET with C# 14
- **[ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/)** – RESTful API with minimal API or controller-based endpoints
- **[Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)** – Local development orchestration and service discovery
- **[Entity Framework Core 10](https://learn.microsoft.com/en-us/ef/core/)** – ORM with Cosmos DB provider
- **[Microsoft Agent Framework](https://github.com/microsoft/agent-framework)** – AI agent orchestration, tools, and capabilities
- **[Azure SDK for .NET](https://learn.microsoft.com/en-us/dotnet/azure/)** – Azure service integrations (Cosmos DB, Blob Storage, Key Vault, AI Services)

## Hosting

### Production Deployment

- **Azure Container Apps** – Managed Kubernetes environment
  - Automatic scaling (0 to N instances based on load)
  - HTTP/HTTPS ingress with managed certificates
  - Integration with VNet for private endpoints
  - Container registry integration for CI/CD
  - Health probes and rolling updates

### Local Development

- **Aspire AppHost** – Local orchestration
  - Single-command startup for entire distributed application
  - Automatic service discovery (services reference each other by name)
  - Connection string injection for Cosmos DB, Redis, etc.
  - Aspire Dashboard for observability (logs, traces, metrics)
  - Docker Compose alternative with better developer experience

## Architecture Layers

The backend follows Clean/Hexagonal Architecture with clear separation of concerns:

```text
libris-maleficarum-service/
├── LibrisMaleficarum.slnx              # Solution file (.NET 9+ XML format)
├── src/
│   ├── Api/                            # HTTP API layer (ASP.NET Core)
│   │   ├── LibrisMaleficarum.Api.csproj
│   │   ├── Controllers/                # API controllers (if using controllers)
│   │   ├── Endpoints/                  # Minimal API endpoints (if using minimal APIs)
│   │   ├── Middleware/                 # Custom middleware
│   │   ├── Filters/                    # Action filters, exception filters
│   │   ├── Extensions/                 # Service registration extensions
│   │   └── Program.cs                  # Application entry point
│   │
│   ├── Domain/                         # Domain layer (business logic)
│   │   ├── LibrisMaleficarum.Domain.csproj
│   │   ├── Entities/                   # Domain entities (WorldEntity, Asset, etc.)
│   │   ├── ValueObjects/               # Value objects (immutable domain concepts)
│   │   ├── Interfaces/                 # Repository and service interfaces
│   │   ├── Events/                     # Domain events
│   │   └── Exceptions/                 # Domain-specific exceptions
│   │
│   ├── Infrastructure/                 # Infrastructure layer (external concerns)
│   │   ├── LibrisMaleficarum.Infrastructure.csproj
│   │   ├── Persistence/                # EF Core DbContext and configurations
│   │   ├── Repositories/               # Repository implementations
│   │   ├── Services/                   # External service integrations
│   │   └── Configurations/             # EF Core entity configurations
│   │
│   └── Orchestration/                  # Aspire orchestration
│       ├── AppHost/                    # Aspire AppHost project
│       │   ├── LibrisMaleficarum.AppHost.csproj
│       │   ├── Program.cs              # AppHost entry point
│       │   └── appsettings.json
│       └── ServiceDefaults/            # Shared service configuration
│           ├── LibrisMaleficarum.ServiceDefaults.csproj
│           └── Extensions.cs           # AddServiceDefaults() extension
│
└── tests/                              # Test projects (see TESTING.md)
    ├── Api.Tests/
    │   └── LibrisMaleficarum.Api.Tests.csproj
    ├── Domain.Tests/
    │   └── LibrisMaleficarum.Domain.Tests.csproj
    ├── Infrastructure.Tests/
    │   └── LibrisMaleficarum.Infrastructure.Tests.csproj
    └── Orchestration.Tests/
        └── LibrisMaleficarum.Orchestration.Tests.csproj
```

### Solution and Project Files

- **Solution**: `LibrisMaleficarum.slnx` (root of service folder)
  - Uses .slnx format (.NET 9+ XML-based solution file) for better source control

- **Project Files**:
  - `LibrisMaleficarum.Api.csproj` - Web API project
  - `LibrisMaleficarum.Domain.csproj` - Domain entities and interfaces (class library)
  - `LibrisMaleficarum.Infrastructure.csproj` - Infrastructure implementations (class library)
  - `LibrisMaleficarum.AppHost.csproj` - Aspire orchestration host
  - `LibrisMaleficarum.ServiceDefaults.csproj` - Shared Aspire service defaults (class library)
  - `LibrisMaleficarum.*.Tests.csproj` - xUnit test projects

- **Project References**:
  - Api → Domain, Infrastructure, ServiceDefaults
  - Infrastructure → Domain
  - AppHost → Api (project reference for orchestration)
  - Test projects → Corresponding source projects

### Layer Responsibilities

**Api Layer**:

- HTTP request/response handling
- Input validation and model binding
- Authentication and authorization
- OpenAPI/Swagger documentation
- CORS and security headers

**Domain Layer**:

- Business logic and rules
- Entity validation
- Domain events
- Repository and service interfaces (no implementations)

**Infrastructure Layer**:

- EF Core DbContext and entity configurations
- Repository pattern implementations
- External service integrations (Azure services)
- Caching, logging, telemetry

**Orchestration Layer**:

- Aspire AppHost for local development
- Service defaults (telemetry, health checks, resilience)
- Container orchestration configuration

## Data Access

### Entity Framework Core with Cosmos DB

- **ORM**: Entity Framework Core 10 with Cosmos DB provider
- **Containers**: WorldEntity, Asset, DeletedWorldEntity (see [DATA_MODEL.md](DATA_MODEL.md))
- **Partition Keys**: Hierarchical partition keys for optimal RU usage and hot partition prevention
- **Repository Pattern**: Abstract data access through repository interfaces

### Repository Pattern

The backend uses the Repository pattern to abstract data access logic:

- **Interfaces**: Define contracts in Domain layer (`IWorldEntityRepository`, `IAssetRepository`)
- **Implementations**: Concrete implementations in Infrastructure layer
- **Benefits**: Testability, maintainability, separation of concerns

## API Design

The backend exposes a RESTful API for frontend consumption. See [API.md](API.md) for complete API specification including:

- RESTful endpoint structure
- Request/response formats
- Authentication and authorization
- Pagination and filtering
- Error handling and status codes

### AG-UI Protocol Integration

Microsoft Agent Framework provides native AG-UI protocol support for CopilotKit frontend integration:

- **Endpoint**: `/api/v1/copilotkit` (Server-Sent Events/WebSocket)
- **Protocol**: AG-UI standardized event types for agent-user communication
- **Features**: Bidirectional communication, shared state, frontend action invocation
- **Implementation**: `MapAGUIAgent<WorldBuilderAgent>()` extension method

## Authentication & Authorization

### User Context

The backend uses `IUserContextService` to abstract user identity and authorization:

- **Interface**: Defines contracts for getting current user ID, email, and world access
- **Stubbed Implementation**: Initial development uses stubbed service returning test user ID
- **Future**: Replace with Entra ID CIAM integration for production authentication

### Authorization Rules

- **World Access**: Users can only access worlds they own (`OwnerId` validation)
- **Entity Access**: Row-level security ensures users only access entities in their worlds
- **Asset Access**: Assets are scoped to worlds, enforcing same ownership rules

## Microsoft Agent Framework Integration

### Agent Architecture

The backend integrates Microsoft Agent Framework for AI-powered world-building assistance:

- **Agent Definition**: `WorldBuilderAgent` with configured instructions and capabilities
- **Tools**: Function calling tools for entity operations (create, search, update)
- **Chat Client**: Integration with Azure AI Services for natural language processing
- **AG-UI Protocol**: Native support for CopilotKit frontend via `MapAGUIAgent()` extension

### Agent Capabilities

- **Entity Management**: Create, update, search entities via conversational interface
- **Content Generation**: Generate descriptions, names, and lore for entities
- **Consistency Checking**: Validate entity relationships and maintain world consistency
- **Contextual Suggestions**: Provide relevant suggestions based on campaign context

## Observability

### Aspire Dashboard

Local development uses Aspire Dashboard for comprehensive observability:

- **Logs**: Structured logging from all services with filtering and search
- **Traces**: Distributed tracing with OpenTelemetry for request flows
- **Metrics**: Performance counters, custom metrics, and resource utilization
- **Resource Health**: Service status, dependencies, and connection states

### Application Insights

Production uses Azure Application Insights for monitoring and diagnostics:

- **Request Telemetry**: HTTP request tracking with response times and status codes
- **Dependency Tracking**: External calls to Cosmos DB, Blob Storage, AI Services
- **Exception Logging**: Automatic exception capture with stack traces
- **Custom Events**: Business metrics and user activity tracking
- **Distributed Tracing**: End-to-end request correlation across services

### Health Checks

ASP.NET Core health checks monitor service dependencies:

- **Liveness**: `/health` - Service is running
- **Readiness**: `/health/ready` - Service ready to accept requests
- **Dependency Checks**: Cosmos DB, Blob Storage, AI Services connectivity

## Configuration

### Configuration Sources

- **appsettings.json**: Default configuration for development
- **Environment Variables**: Production secrets and connection strings
- **Azure Key Vault**: Sensitive configuration (managed via Aspire integration)
- **Aspire Configuration**: Service discovery and dependency injection

### Configuration Structure

Configuration organized by concern:

- **CosmosDb**: Endpoint, database name, connection settings
- **BlobStorage**: Connection string, container names, SAS token expiry
- **AzureAI**: Endpoint, deployment name, model configuration
- **Authentication**: JWT settings, Entra ID configuration (future)
- **Logging**: Log levels, Application Insights instrumentation key

### Aspire Service Integration

Aspire simplifies local development with automatic service configuration:

- **Service Discovery**: Services reference each other by name (no hardcoded URLs)
- **Connection Injection**: Automatic connection string management
- **Resource Management**: Container lifecycle and orchestration
- **Environment Parity**: Consistent configuration across dev/staging/prod

## Deployment

### Container Build

The backend is containerized using multi-stage Docker builds for optimal image size and security:

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:10.0` (SDK for compilation)
- **Multi-Stage**: Separate build and runtime stages for minimal final image
- **Non-Root User**: Container runs as non-root for security

### Azure Container Apps Deployment

The API is deployed to Azure Container Apps using Bicep with AVM modules:

- **Environment**: Deployed to Container Apps Environment (shared across services)
- **Ingress**: External HTTP/HTTPS ingress with automatic certificate management
- **Scaling**: CPU-based autoscaling (0 to N instances)
- **Managed Identity**: System-assigned managed identity for Azure service authentication
- **Private Networking**: VNet integration with private endpoints for backend services

### CI/CD Pipeline

Continuous deployment via GitHub Actions (see [CI_CD.md](CI_CD.md)):

- **Build**: Compile .NET solution, run tests, build container image
- **Push**: Push container image to Azure Container Registry
- **Deploy**: Update Container App with new image via Bicep deployment
- **Health Check**: Verify deployment health before completing

## Future Enhancements

- **GraphQL API**: Consider GraphQL for complex entity graph queries
- **SignalR**: Real-time updates for collaborative editing
- **Caching**: Redis for query result caching
- **Rate Limiting**: Per-user API rate limiting
- **API Versioning**: Versioned endpoints for backward compatibility
