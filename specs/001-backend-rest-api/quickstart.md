# Quick Start Guide: Backend REST API Development

**Last Updated**: 2026-01-03  
**Purpose**: Get developers up and running with the backend REST API in under 10 minutes

## Prerequisites

- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Visual Studio Code** or **Visual Studio 2022** (17.12+)
- **Docker Desktop**: Required for Cosmos DB Emulator (Aspire dependency)
- **Git**: For cloning repository

Optional but recommended:

- **Azure Data Studio** or **Cosmos DB Explorer**: For viewing Cosmos DB data
- **Postman** or **Thunder Client** (VS Code extension): For API testing

## Getting Started (5 Minutes)

### 1. Clone Repository

```powershell
git clone https://github.com/PlagueHO/libris-maleficarum.git
cd libris-maleficarum
git checkout 001-backend-rest-api
```

### 2. Restore Dependencies

```powershell
cd libris-maleficarum-service
dotnet restore LibrisMaleficarum.slnx
```

### 3. Start Aspire AppHost

This single command starts both the API and Cosmos DB Emulator:

```powershell
dotnet run --project src/Orchestration/AppHost
```

**What happens:**

1. Aspire downloads and starts Cosmos DB Emulator container (first run only, ~2 minutes)
1. API service starts and connects to Cosmos DB
1. React frontend starts on https://localhost:4000
1. Aspire Dashboard opens in browser at https://localhost:15888

**Expected Output:**

```text
info: Aspire.Hosting.DistributedApplication[0]
      Aspire version: 13.1.0
info: Aspire.Hosting.DistributedApplication[0]
      Distributed application starting.
info: Aspire.Hosting.DistributedApplication[0]
      Application host directory is: D:\source\GitHub\PlagueHO\libris-maleficarum\libris-maleficarum-service\src\Orchestration\AppHost
info: Aspire.Hosting.DistributedApplication[0]
      Now listening on: https://localhost:15888
info: Aspire.Hosting.DistributedApplication[0]
      Login to the dashboard at https://localhost:15888
```

### 4. Verify Services

Open Aspire Dashboard: **https://localhost:15888**

**Check service status:**

- ✅ **cosmosdb** - Running (green)
- ✅ **api** - Running (green)
- ✅ **frontend** - Running (green)

**Get API URL** from dashboard:

- Click **api** → **Details** → Copy endpoint URL (typically `https://localhost:7041`)

### 5. Test API (Choose One Method)

**Option A: Swagger UI**

1. Navigate to: `https://localhost:7041/swagger`
1. Expand **POST /api/v1/worlds**
1. Click **Try it out**
1. Enter request body:

   ```json
   {
     "name": "Test World",
     "description": "My first world"
   }
   ```

1. Click **Execute**
1. Verify **201 Created** response

**Option B: PowerShell (curl)**

```powershell
$headers = @{ "Content-Type" = "application/json" }
$body = @{
    name = "Test World"
    description = "My first world"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7041/api/v1/worlds" `
    -Method POST `
    -Headers $headers `
    -Body $body `
    -SkipCertificateCheck

$response
```

**Expected Response:**

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "ownerId": "00000000-0000-0000-0000-000000000001",
    "name": "Test World",
    "description": "My first world",
    "createdDate": "2026-01-03T10:30:00Z",
    "modifiedDate": "2026-01-03T10:30:00Z"
  },
  "meta": {
    "requestId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "timestamp": "2026-01-03T10:30:00Z"
  }
}
```

**Option C: VS Code REST Client**

Create `test.http` file:

```http
### Create World
POST https://localhost:7041/api/v1/worlds
Content-Type: application/json

{
  "name": "Test World",
  "description": "My first world"
}

### Get All Worlds
GET https://localhost:7041/api/v1/worlds

### Get Specific World
GET https://localhost:7041/api/v1/worlds/{{worldId}}
```

Click **Send Request** above each endpoint.

---

## Development Workflow

### Running Tests

```powershell
# Run all tests
dotnet test LibrisMaleficarum.slnx

# Run tests for specific project
dotnet test tests/Api.Tests/LibrisMaleficarum.Api.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Building Solution

```powershell
# Build all projects
dotnet build LibrisMaleficarum.slnx

# Build in Release mode
dotnet build LibrisMaleficarum.slnx --configuration Release
```

### Viewing Cosmos DB Data

**Option 1: Aspire Dashboard**

1. Open https://localhost:15888
1. Click **cosmosdb** → **View Data**
1. Browse containers: Worlds, WorldEntities, Assets

**Option 2: Cosmos DB Explorer (Container)**

```powershell
# Get Cosmos DB Emulator endpoint from Aspire
# Typically: https://localhost:8081

# Connect using Azure Data Studio or Cosmos DB Explorer
# Connection String: AccountEndpoint=https://localhost:8081;AccountKey=<emulator-key>
```

### Debugging in VS Code

**Launch Configuration** (`.vscode/launch.json`):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API via Aspire",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "aspire: run",
      "program": "${workspaceFolder}/libris-maleficarum-service/src/Api/bin/Debug/net10.0/LibrisMaleficarum.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/libris-maleficarum-service/src/Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

**Set Breakpoints**:

1. Open `src/Api/Controllers/WorldsController.cs`
1. Set breakpoint on line in `CreateWorld` method
1. Press **F5** to start debugging
1. Send POST request to `/api/v1/worlds`
1. Breakpoint hits → inspect variables

---

## Project Structure Navigation

```text
libris-maleficarum-service/
├── src/
│   ├── Api/                          # Start here for API endpoints
│   │   └── Controllers/WorldsController.cs  # World Management REST API
│   ├── Domain/                       # Business logic and entities
│   │   ├── Entities/World.cs         # World domain entity
│   │   └── Interfaces/Repositories/IWorldRepository.cs  # Repository contract
│   ├── Infrastructure/               # Data access and external services
│   │   ├── Persistence/ApplicationDbContext.cs  # EF Core DbContext
│   │   └── Repositories/WorldRepository.cs  # Repository implementation
│   └── Orchestration/AppHost/AppHost.cs  # Aspire service definitions
└── tests/
    ├── Api.Tests/Controllers/WorldsControllerTests.cs  # Controller unit tests
    ├── Domain.Tests/Entities/WorldTests.cs  # Domain logic tests
    └── Infrastructure.Tests/Repositories/WorldRepositoryTests.cs  # Repo tests
```

**Key Files to Understand**:

1. **AppHost.cs** - Aspire orchestration (Cosmos DB + API + Frontend)
1. **WorldsController.cs** - REST API endpoints for World Management
1. **IWorldRepository.cs** - Repository interface (Domain layer)
1. **WorldRepository.cs** - EF Core implementation (Infrastructure layer)
1. **ApplicationDbContext.cs** - Cosmos DB configuration
1. **Program.cs** (Api project) - Service registration and middleware pipeline

---

## Common Tasks

### Add New API Endpoint

1. **Define Request/Response DTOs** in `src/Api/Models/`
1. **Add Controller Method**:

   ```csharp
   [HttpGet("{worldId}/details")]
   [ProducesResponseType<ApiResponse<WorldDetailsResponse>>(200)]
   public async Task<IActionResult> GetWorldDetails([FromRoute] Guid worldId)
   {
       // Implementation
   }
   ```

1. **Write Test First** (TDD):

   ```csharp
   [Fact]
   public async Task GetWorldDetails_WithValidId_ReturnsDetails()
   {
       // Arrange, Act, Assert
   }
   ```

1. **Run Test** → Fails (Red)
1. **Implement Logic** → Test Passes (Green)
1. **Refactor** if needed

### Add New Domain Entity

1. **Create Entity Class** in `src/Domain/Entities/`
1. **Create Repository Interface** in `src/Domain/Interfaces/Repositories/`
1. **Create Repository Implementation** in `src/Infrastructure/Repositories/`
1. **Add EF Core Configuration** in `src/Infrastructure/Persistence/Configurations/`
1. **Register in DbContext**:

   ```csharp
   public DbSet<NewEntity> NewEntities { get; set; }
   ```

1. **Update Container Configuration**:

   ```csharp
   modelBuilder.Entity<NewEntity>()
       .ToContainer("NewEntities")
       .HasPartitionKey(e => e.WorldId);
   ```

### Change Cosmos DB Connection

**Local Development** (via Aspire - automatic):

- Connection string managed by Aspire
- No manual configuration needed

**Production** (future):

- Set environment variable: `ConnectionStrings__CosmosDb`
- Or use Azure Key Vault reference in `appsettings.Production.json`

---

## Troubleshooting

### Cosmos DB Emulator Won't Start

**Symptom**: Aspire shows cosmosdb service as "Failed"

**Solutions**:

1. Ensure Docker Desktop is running
1. Clear Docker containers:

   ```powershell
   docker ps -a
   docker rm $(docker ps -aq)
   ```

1. Restart Aspire AppHost

### API Returns 503 Service Unavailable

**Symptom**: All endpoints return 503

**Cause**: Cosmos DB not connected

**Solution**:

1. Check Aspire Dashboard → cosmosdb status
1. Wait for Cosmos DB Emulator to fully start (~30 seconds first run)
1. Restart API service from Aspire Dashboard

### Tests Fail with "Collection was modified"

**Cause**: Concurrent test execution modifying shared state

**Solution**:

- Use `IAsyncLifetime` for test fixtures
- Isolate test data (unique GUIDs per test)
- Avoid static/shared state

### Swagger UI Shows 404

**Symptom**: `/swagger` returns 404

**Cause**: Swagger only enabled in Development environment

**Solution**:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Api
```

---

## Useful Commands

```powershell
# Start Aspire (all services)
dotnet run --project src/Orchestration/AppHost

# Run API only (no Aspire)
dotnet run --project src/Api

# Run tests in watch mode
dotnet watch test --project tests/Api.Tests

# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage/report

# Format code
dotnet format LibrisMaleficarum.slnx

# Clean build artifacts
dotnet clean LibrisMaleficarum.slnx
```

---

## Next Steps

1. ✅ **Read** [data-model.md](data-model.md) for entity schemas
1. ✅ **Review** [contracts/worlds.yaml](contracts/worlds.yaml) for API contracts
1. ✅ **Explore** Aspire Dashboard for observability
1. ✅ **Write** your first test following AAA pattern
1. ✅ **Implement** World Management endpoints (Priority 1)

## Additional Resources

- [Aspire.NET Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [EF Core Cosmos DB Provider](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/)
- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator)

---

**Questions?** Check [spec.md](spec.md) for requirements or [research.md](research.md) for technology decisions.
