# Infrastructure Integration Tests

This project contains integration tests for the LibrisMaleficarum infrastructure layer, including tests that interact with real Azure Cosmos DB (via the Cosmos DB Emulator) and other Aspire-managed services.

## AppHostFixture

The `AppHostFixture` class is provided by the `LibrisMaleficarum.IntegrationTests.Shared` project and creates a shared Aspire AppHost instance that can be reused across all integration test classes. This fixture:

- Creates a single AppHost instance that is shared across all test classes
- Automatically starts the Cosmos DB emulator and waits for it to be ready
- Prevents Docker resource conflicts and port collisions
- Significantly improves test performance by avoiding repeated AppHost initialization

### Usage Pattern

To use the `AppHostFixture` in your integration test class:

```csharp
using LibrisMaleficarum.IntegrationTests.Shared;

[TestClass]
[TestCategory("Integration")]
[TestCategory("RequiresDocker")]
[DoNotParallelize] // Required to avoid port conflicts
public class MyIntegrationTests
{
    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await AppHostFixture.InitializeAsync(context);
    }

    [TestMethod]
    public void MyTest()
    {
        // Access the shared AppHost via AppHostFixture.App
        AppHostFixture.App.Should().NotBeNull();
        
        // Get connection strings, create HTTP clients, etc.
        var connectionString = await AppHostFixture.App!.GetConnectionStringAsync("cosmosdb");
        
        TestContext?.WriteLine("[TEST] Test running...");
    }
}
```

### Key Points

- **Thread-safe initialization**: The fixture ensures only one AppHost is created, even when multiple test classes initialize concurrently
- **ClassInitialize only**: Call `AppHostFixture.InitializeAsync()` in your `[ClassInitialize]` method
- **No ClassCleanup needed**: The AppHost is shared across all test classes and will be cleaned up at assembly cleanup time
- **Access via static property**: Use `AppHostFixture.App` to access the shared `DistributedApplication` instance
- **30-second startup wait**: The fixture automatically waits 30 seconds for the Cosmos DB emulator to be ready
-  Project Dependencies

This project references `LibrisMaleficarum.IntegrationTests.Shared` which provides the `AppHostFixture` and other shared test infrastructure.

### For Test Classes in Other Projects

Test classes in other projects can use the `AppHostFixture` by adding a project reference to `LibrisMaleficarum.IntegrationTests.Shared`:
1. Add a project reference to `LibrisMaleficarum.Infrastructure.IntegrationTests`
2. Use the fully qualified name: `LibrisMaleficarum.Infrastructure.IntegrationTests.AppHostFixture`
xml
<ItemGroup>
  <ProjectReference Include="..\IntegrationTests.Shared\LibrisMaleficarum.IntegrationTests.Shared.csproj" />
</ItemGroup>
```

Then use it in your test class:

```csharp
using LibrisMaleficarum.IntegrationTests.Shared;

[ClassInitialize]
public static async Task ClassInitialize(TestContext context)
{
    await AppHostFixture.InitializeAsync(context);
}

[TestMethod]
public void MyTest()
{
    var app = 
    var app = LibrisMaleficarum.Infrastructure.IntegrationTests.AppHostFixture.App;
    // ... test code
}
```

## Running Integration Tests

Integration tests require Docker to be running (for the Cosmos DB emulator). To run all integration tests:

```bash
dotnet test --filter "TestCategory=Integration"
```

To run a specific test class:

```bash
dotnet test --filter "FullyQualifiedName~MinimalAppHostTests"
```

## Test Categories

- **Integration**: Tests that require external dependencies (Docker, Cosmos DB, etc.)
- **RequiresDocker**: Tests that specifically require Docker to be running

## Performance

Using the shared `AppHostFixture`:
- **First test class**: ~50-60 seconds (includes AppHost initialization + 30s Cosmos DB wait + test execution)
- **Subsequent test classes**: Faster (reuses existing AppHost, no initialization overhead)
- **Per-test pattern (old)**: 120+ seconds for 3 tests (40s × 3)

The shared fixture pattern reduces test execution time by approximately 50%.
