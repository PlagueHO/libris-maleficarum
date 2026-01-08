# Libris Maleficarum Service Test Projects

This directory contains all test projects for the Libris Maleficarum backend service.

## Test Project Structure

All test projects follow a **strict separation** between unit tests and integration tests:

```text
tests/
  ├── Api.Tests/                        # Unit tests (mocked dependencies)
  ├── Api.IntegrationTests/             # Integration tests (real HTTP, real DB)
  ├── Domain.Tests/                     # Unit tests (pure logic, no dependencies)
  ├── Infrastructure.Tests/             # Unit tests (mocked repositories)
  ├── Infrastructure.IntegrationTests/  # Integration tests (real Cosmos DB via Docker)
  └── Orchestration.IntegrationTests/   # Integration tests (AppHost orchestration)
```

## Naming Convention

- **Unit Tests**: `{Layer}.Tests`
  - Fast, isolated, mocked dependencies
  - Run on every build

- **Integration Tests**: `{Layer}.IntegrationTests`
  - Slower, real dependencies, requires Docker
  - Run on PR/merge

## Key Principles

### Separation of Concerns

| Aspect | Unit Tests | Integration Tests |
| ------ | ---------- | ----------------- |
| **Project Name** | `*.Tests` | `*.IntegrationTests` |
| **Dependencies** | Mocked (NSubstitute, EF InMemory) | Real (Cosmos DB, HTTP, Docker) |
| **Execution Time** | Milliseconds | 20-40 seconds per test |
| **Test Category** | None or `[TestCategory("Unit")]` | `[TestCategory("Integration")]` |
| **Docker Required** | No | Yes |
| **CI/CD Frequency** | Every commit | PR/Merge only |
| **Isolation** | Per-method via mocks | Per-method via AppHost |

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

### All Tests

```powershell
dotnet test
```

### Specific Project

```powershell
dotnet test --project tests/Infrastructure.Tests/
dotnet test --project tests/Infrastructure.IntegrationTests/
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

```csharp
[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // REQUIRED - prevents port conflicts
public class MyIntegrationTests
{
    public required TestContext TestContext { get; init; }

    [TestMethod]
    public async Task MyIntegrationTest()
    {
        // Arrange - Create AppHost per test (isolated, repeatable)
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
        
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        
        // Get real connection string
        var connectionString = await app.GetConnectionStringAsync("cosmosdb");
        
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

### 3. No Shared Fixtures

**Do NOT use shared fixtures** (ClassInitialize, AssemblyInitialize) with AppHost.

❌ **Bad:**

```csharp
[ClassInitialize]
public static async Task Initialize(TestContext context)
{
    // Shared AppHost - causes issues with async initialization
    _sharedApp = await DistributedApplicationTestingBuilder.CreateAsync<...>();
}
```

✅ **Good:**

```csharp
[TestMethod]
public async Task MyTest()
{
    // Per-test AppHost - fully isolated, reliable
    var appHost = await DistributedApplicationTestingBuilder.CreateAsync<...>();
    await using var app = await appHost.BuildAsync();
    await app.StartAsync();
}
```

**Rationale**: MSTest.Sdk has issues with async static initialization. Per-test AppHost creation
ensures complete isolation and matches Aspire best practices.

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
<PackageReference Include="Aspire.Hosting.Testing" Version="13.1.0" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<!-- Add real providers only when needed -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Cosmos" Version="10.0.0" />
```

## Standard Across Solution

This testing structure is **mandatory** for all layers:

- ✅ `Api.Tests` + `Api.IntegrationTests`
- ✅ `Domain.Tests` (unit only - pure logic, no integration needs)
- ✅ `Infrastructure.Tests` + `Infrastructure.IntegrationTests`
- ✅ `Orchestration.IntegrationTests` (integration only - tests AppHost itself)

## Best Practices

### Unit Tests

1. **Fast**: Each test should complete in milliseconds
1. **Isolated**: Use mocks for all external dependencies
1. **Focused**: Test one logical unit per test
1. **Deterministic**: No random values, no time dependencies
1. **Readable**: Use clear Arrange-Act-Assert structure

### Integration Tests

1. **Per-Test AppHost**: Create new AppHost in each test method
1. **Sequential Execution**: Always use `[DoNotParallelize]`
1. **Docker Cleanup**: AppHost handles cleanup via `await using`
1. **Diagnostic Output**: Use `TestContext.WriteLine()` for debugging
1. **Realistic**: Test against real infrastructure, not mocks

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
