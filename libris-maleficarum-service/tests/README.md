# Libris Maleficarum - Backend Service - Tests

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
  ├── IntegrationTests.Shared/          # Shared test fixtures (AppHostFixture)
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
