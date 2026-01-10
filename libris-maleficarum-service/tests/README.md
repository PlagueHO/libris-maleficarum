# Libris Maleficarum - Backend Service - Tests

This directory contains all test projects for the Libris Maleficarum backend service.

## Test Project Structure

All test projects follow a **strict separation** between unit tests and integration tests:

```text
tests/
  ├── unit/
  │   ├── Api.Tests/                    # Unit tests (mocked dependencies)
  │   ├── Domain.Tests/                 # Unit tests (pure logic, no dependencies)
  │   └── Infrastructure.Tests/         # Unit tests (mocked repositories)
  ├── integration/
  │   ├── Api.IntegrationTests/         # Integration tests (real HTTP, real DB)
  │   ├── Infrastructure.IntegrationTests/  # Integration tests (real Cosmos DB via Docker)
  │   ├── IntegrationTests.Shared/      # Shared test fixtures (AppHostFixture)
  │   └── Orchestration.IntegrationTests/   # Integration tests (AppHost orchestration)
  └── performance/
      └── Performance.Tests/            # Performance tests (load testing, p95 metrics)
```

## Naming Convention

- **Unit Tests**: `{Layer}.Tests`
  - Fast, isolated, mocked dependencies
  - Run on every build

- **Integration Tests**: `{Layer}.IntegrationTests`
  - Slower, real dependencies, requires Docker
  - Run on PR/merge

- **Performance Tests**: `Performance.Tests`
  - Performance metrics, load testing, p95 response times
  - Run manually or on-demand only (not in regular CI/CD)

## Key Principles

### Separation of Concerns

| Aspect | Unit Tests | Integration Tests | Performance Tests |
| ------ | ---------- | ----------------- | ----------------- |
| **Project Name** | `*.Tests` | `*.IntegrationTests` | `Performance.Tests` |
| **Dependencies** | Mocked (NSubstitute, EF InMemory) | Real (Cosmos DB, HTTP, Docker) | Real (Cosmos DB, HTTP, Docker) |
| **Execution Time** | Milliseconds | 20-40 seconds per test | Minutes (1000+ entities) |
| **Test Category** | None or `[TestCategory("Unit")]` | `[TestCategory("Integration")]` | `[TestCategory("Performance")]` |
| **Docker Required** | No | Yes | Yes |
| **CI/CD Frequency** | Every commit | PR/Merge only | On-demand/Manual only |
| **Isolation** | Per-method via mocks | Per-method via AppHost | Per-method via AppHost |

### Why Separate Projects?

1. **Dependency Isolation**: Integration tests require `Aspire.Hosting.Testing`, Cosmos DB provider, etc.;
   unit tests use minimal mocking libraries
1. **Performance**: Developers get fast feedback from unit tests without waiting for Docker containers
1. **CI/CD Flexibility**: Run unit tests on every commit; integration tests only on PR
1. **Clarity**: Project structure immediately communicates test type and requirements

## Running Tests

### Unit Tests Only (Fast Feedback)

```powershell
dotnet test --filter TestCategory!=Integration
```

### Integration Tests Only

```powershell
dotnet test --filter TestCategory=Integration
```

### Performance Tests Only (Manual/On-Demand)

```powershell
dotnet test --filter TestCategory=Performance
# or target specific project
dotnet test --project tests/performance/LibrisMaleficarum.Performance.Tests.csproj --filter TestCategory=Performance
```

### All Tests

```powershell
dotnet test
```

### Specific Project

```powershell
dotnet test --project tests/unit/Infrastructure.Tests/
dotnet test --project tests/integration/Infrastructure.IntegrationTests/
```

## Unit Test Pattern

```csharp
[TestClass]
public class MyServiceTests
{
    [TestMethod]
    public async Task MyMethod_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var mockDependency = Substitute.For<IDependency>();
        mockDependency.DoSomething().Returns(expectedValue);
        var sut = new MyService(mockDependency);
        
        // Act
        var result = await sut.MyMethod();
        
        // Assert
        result.Should().Be(expectedValue);
        mockDependency.Received(1).DoSomething();
    }
}
```

## Integration Test Pattern

Integration tests use the shared `AppHostFixture` from `IntegrationTests.Shared` project to manage the Aspire AppHost lifecycle.

```csharp
using LibrisMaleficarum.IntegrationTests.Shared;

[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // REQUIRED - prevents port conflicts
public class MyIntegrationTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Initialize shared AppHost (runs once per test class)
        await AppHostFixture.InitializeAsync(context);
    }

    [TestMethod]
    public async Task MyIntegrationTest()
    {
        // Arrange - Use shared AppHost instance
        AppHostFixture.App.Should().NotBeNull();
        
        // Get real connection string from shared AppHost
        var connectionString = await AppHostFixture.App!.GetConnectionStringAsync("cosmosdb");
        
        // Create real DbContext
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseCosmos(connectionString, "DatabaseName")
            .Options;
        
        await using var context = new MyDbContext(options);
        
        // Act - Test against real infrastructure
        // ...
        
        // Assert - Verify behavior with real dependencies
        // ...
    }
}
```

## Critical: Integration Test Requirements

### 1. Sequential Execution Required

**All integration test classes MUST use `[DoNotParallelize]`**.

```csharp
[TestClass]
[DoNotParallelize] // Prevents port conflicts from parallel AppHost instances
public class MyIntegrationTests
{
    // Tests execute sequentially
}
```

**Why?** Each AppHost creates Docker containers binding to specific ports (Cosmos DB → 8081,
health → 8080). Parallel execution causes:

- Port binding conflicts
- Test hangs/freezes
- Unpredictable failures

### 2. Test Categories

All integration tests must use both categories:

```csharp
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
```

This enables:

- Filtering in CI/CD pipelines
- Clear documentation of requirements
- IDE test runner organization

### 3. Shared AppHost Fixture

**All integration tests MUST use the shared `AppHostFixture`** from the `IntegrationTests.Shared` project.

✅ **Correct Pattern:**

```csharp
using LibrisMaleficarum.IntegrationTests.Shared;

[TestClass]
public class MyIntegrationTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    [TestMethod]
    public async Task MyTest()
    {
        // Access shared AppHost instance
        var connectionString = await AppHostFixture.App!.GetConnectionStringAsync("cosmosdb");
    }
}
```

**Benefits:**

- **Single AppHost instance** shared across all integration test classes
- **Eliminates Docker resource conflicts** from parallel AppHost creation
- **Improves performance** by avoiding repeated 30-second Cosmos DB initialization
- **Thread-safe initialization** ensures only one AppHost is created
- **Consistent test environment** across all integration tests

**Project Reference Required:**

```xml
<ItemGroup>
  <ProjectReference Include="..\IntegrationTests.Shared\LibrisMaleficarum.IntegrationTests.Shared.csproj" />
</ItemGroup>
```

### 4. AppHost Resources

The shared `AppHostFixture` provides access to multiple infrastructure resources managed by Aspire:

#### Available Resources

| Resource | Access Method | Description | Port |
| -------- | ------------- | ----------- | ---- |
| **Cosmos DB Emulator** | `AppHostFixture.CosmosDbConnectionString` | Azure Cosmos DB Emulator for database operations | 8081 |
| **Cosmos DB Endpoint** | `AppHostFixture.CosmosDbAccountEndpoint` | Cosmos DB account endpoint URL | - |
| **Cosmos DB Key** | `AppHostFixture.CosmosDbAccountKey` | Cosmos DB account key for authentication | - |
| **Azurite Storage** | `AppHostFixture.StorageConnectionString` | Azure Storage Emulator for blob/queue/table operations | 10000-10002 |
| **API Service** | `AppHostFixture.App.CreateHttpClient("api")` | Backend API service instance | Dynamic |
| **API Base URL** | `AppHostFixture.ApiBaseUrl` | Base URL for API service | - |

#### Usage Examples

**Cosmos DB Access:**

```csharp
[TestMethod]
public async Task MyCosmosDbTest()
{
    // Get connection string from shared AppHost
    var connectionString = AppHostFixture.CosmosDbConnectionString;
    
    // Create DbContext with real Cosmos DB Emulator
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseCosmos(connectionString, "LibrisMaleficarum")
        .Options;
    
    await using var context = new ApplicationDbContext(options);
    
    // Test against real Cosmos DB
    var world = World.Create(ownerId, "Test World", "Description");
    context.Worlds.Add(world);
    await context.SaveChangesAsync();
}
```

**Azure Storage Access:**

```csharp
[TestMethod]
public async Task MyBlobStorageTest()
{
    // Get storage connection string from shared AppHost
    var connectionString = AppHostFixture.StorageConnectionString;
    
    // Create BlobServiceClient with real Azurite Emulator
    var blobServiceClient = new BlobServiceClient(connectionString);
    var blobStorageService = new BlobStorageService(blobServiceClient);
    
    // Test against real blob storage
    using var stream = new MemoryStream(fileContent);
    var blobUrl = await blobStorageService.UploadAsync(
        "test-container", 
        "test-blob.txt", 
        stream, 
        "text/plain");
}
```

**API HTTP Client Access:**

```csharp
[TestMethod]
public async Task MyApiTest()
{
    // Create HTTP client for API service
    using var httpClient = AppHostFixture.App!.CreateHttpClient("api");
    
    // Make requests to real API
    var request = new { Name = "Test World", Description = "Test" };
    using var response = await httpClient.PostAsJsonAsync("/api/v1/worlds", request);
    
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

#### Resource Lifecycle

- **Initialization**: Resources are started once during `AppHostFixture.InitializeAsync()`
- **Health Checks**: All resources wait for health status before tests run
- **Shared State**: All test classes share the same resource instances
- **Cleanup**: Resources are cleaned up during test assembly cleanup
- **Port Binding**: Fixed ports (Cosmos DB → 8081) prevent randomization conflicts

#### Prerequisites

**Docker Desktop Required:**

All integration tests require Docker Desktop to be running with the following containers:

- `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest` (Cosmos DB)
- `mcr.microsoft.com/azure-storage/azurite:latest` (Azurite Storage)

**Verification:**

```powershell
# Verify Docker is running
docker ps

# Check if containers are started during test run
docker ps | Select-String -Pattern "cosmosdb|azurite"
```

**Troubleshooting:**

- **"Port already in use"**: Stop orphaned containers with `docker ps -a` and `docker rm -f <container-id>`
- **Health check timeout**: Increase timeout in AppHostFixture initialization (default: 120 seconds)
- **Cosmos DB initialization slow**: First run downloads emulator image (~1.5GB); subsequent runs are faster

## Test Dependencies

### Unit Test Projects

```xml
<PackageReference Include="MSTest.Sdk" Version="3.7.0" />
<PackageReference Include="NSubstitute" Version="5.3.0" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

### Integration Test Projects

```xml
<PackageReference Include="MSTest.Sdk" Version="3.7.0" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<!-- Add real providers only when needed -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Cosmos" Version="10.0.0" />

<!-- Reference shared fixtures project for AppHostFixture -->
<ProjectReference Include="..\IntegrationTests.Shared\LibrisMaleficarum.IntegrationTests.Shared.csproj" />
```

### IntegrationTests.Shared Project

The shared fixtures library contains common test infrastructure:

```xml
<PackageReference Include="MSTest.TestFramework" Version="3.7.0" />
<PackageReference Include="Aspire.Hosting.Testing" Version="13.1.0" />

<ProjectReference Include="..\..\src\Orchestration\AppHost\LibrisMaleficarum.AppHost.csproj" />
```

## Standard Across Solution

This testing structure is **mandatory** for all layers:

- ✅ `Api.Tests` + `Api.IntegrationTests`
- ✅ `Domain.Tests` (unit only - pure logic, no integration needs)
- ✅ `Infrastructure.Tests` + `Infrastructure.IntegrationTests`
- ✅ `Orchestration.IntegrationTests` (integration only - tests AppHost itself)
- ✅ `IntegrationTests.Shared` (shared fixtures - AppHostFixture for all integration tests)
- ✅ `Performance.Tests` (performance/load tests - run on-demand only)

## Best Practices

### Unit Tests

1. **Fast**: Each test should complete in milliseconds
1. **Isolated**: Use mocks for all external dependencies
1. **Focused**: Test one logical unit per test
1. **Deterministic**: No random values, no time dependencies
1. **Readable**: Use clear Arrange-Act-Assert structure

### Integration Tests

1. **Shared AppHost Fixture**: Use `AppHostFixture` from `IntegrationTests.Shared` project
1. **ClassInitialize**: Call `await AppHostFixture.InitializeAsync(context)` in `[ClassInitialize]`
1. **Sequential Execution**: Always use `[DoNotParallelize]`
1. **Access via Property**: Use `AppHostFixture.App` to access the shared instance
1. **Diagnostic Output**: Use `TestContext.WriteLine()` for debugging
1. **Realistic**: Test against real infrastructure, not mocks

### Performance Tests

1. **Test Category**: Always mark with `[TestCategory("Performance")]`
1. **Explicit Execution**: Never run by default - require explicit filter
1. **Metrics Logging**: Log detailed performance metrics (min, avg, p95, max)
1. **Realistic Load**: Test with production-like data volumes (100s-1000s of entities)
1. **Thresholds**: Assert against performance requirements (e.g., p95 <200ms)
1. **Documentation**: See `tests/performance/README.md` for detailed usage

## Troubleshooting

### Tests Hang/Freeze

- **Cause**: Missing `[DoNotParallelize]` attribute
- **Solution**: Add attribute to test class

### "Port already in use" Errors

- **Cause**: Previous test didn't clean up Docker containers
- **Solution**: Run `docker ps` and stop orphaned containers

### Slow Test Execution

- **Cause**: Running integration tests on every build
- **Solution**: Use `--filter TestCategory!=Integration` during development

### Build Errors "File is locked"

- **Cause**: Hung test process from previous run
- **Solution**: Kill hung processes:
  `Get-Process | Where-Object {$_.ProcessName -like "*testhost*"} | Stop-Process -Force`

## CI/CD Recommendations

### PR Pipeline

```yaml
- name: Unit Tests
  run: dotnet test --filter TestCategory!=Integration

- name: Integration Tests
  run: dotnet test --filter TestCategory=Integration
  condition: success()
```

### Main Branch

```yaml
- name: All Tests
  run: dotnet test
```

This ensures fast feedback on PRs while maintaining comprehensive coverage on merge.
