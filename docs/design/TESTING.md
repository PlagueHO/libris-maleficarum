# Automated Test Approaches

A comprehensive automated testing strategy ensures code quality, maintainability, and reliability across both frontend and backend layers.

## Frontend Testing (React + TypeScript)

### Test Framework Stack

- **Test Runner**: Vitest (fast, ESM-native, Vite-compatible)
- **Component Testing**: React Testing Library + @testing-library/jest-dom
- **Accessibility Testing**: jest-axe (WCAG 2.2 Level AA validation)
- **API Mocking**: MSW (Mock Service Worker) v2.x
- **E2E Testing**: Playwright (recommended) or Cypress

### Test Organization

```text
libris-maleficarum-app/
├── src/
│   ├── components/
│   │   └── MyComponent/
│   │       ├── MyComponent.tsx
│   │       └── MyComponent.test.tsx      # Co-located component tests
│   ├── hooks/
│   │   └── useMyHook.test.ts             # Hook tests
│   └── __tests__/
│       ├── a11y.test.tsx                 # Cross-cutting accessibility tests
│       └── integration.test.tsx          # Integration tests
└── test-results/                          # Test output artifacts
```

### Testing Patterns

**Component Tests** (follow established patterns in `src/components/*/*.test.tsx`):

```typescript
import { render, screen } from '@testing-library/react';
import { axe } from 'jest-axe';
import { Provider } from 'react-redux';
import { store } from '../store/store';
import MyComponent from './MyComponent';

describe('MyComponent', () => {
  it('renders with correct role and label', () => {
    render(
      <Provider store={store}>
        <MyComponent />
      </Provider>
    );
    
    const element = screen.getByRole('button', { name: 'Submit' });
    expect(element).toBeInTheDocument();
  });
  
  it('has no accessibility violations', async () => {
    const { container } = render(
      <Provider store={store}>
        <MyComponent />
      </Provider>
    );
    
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```

**Redux State Tests**:

```typescript
import { store } from '../store/store';
import { sliceAction } from '../store/mySlice';

describe('mySlice', () => {
  it('updates state correctly', () => {
    store.dispatch(sliceAction());
    const state = store.getState();
    expect(state.mySlice.value).toBe(expectedValue);
  });
});
```

**API Mocking with MSW**:

```typescript
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';

const server = setupServer(
  http.get('/api/worlds/:id', () => {
    return HttpResponse.json({ id: '123', name: 'Test World' });
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

### Accessibility Requirements

- **All component tests MUST include accessibility validation** using `jest-axe`
- Target: WCAG 2.2 Level AA compliance
- Test keyboard navigation, ARIA attributes, and screen reader compatibility
- See `src/__tests__/a11y.test.tsx` for examples

### Test Execution

```bash
pnpm test              # Run tests in watch mode
pnpm test:ui           # Run Vitest UI (browser-based test runner)
pnpm test:coverage     # Generate coverage report
```

### Coverage Targets

- **Statements**: 80% minimum
- **Branches**: 75% minimum
- **Functions**: 80% minimum
- **Lines**: 80% minimum

## Backend Testing (.NET 10 + Aspire + EF Core)

### Test Framework Stack

- **Test Framework**: xUnit 2.9+ (latest stable version)
- **Assertions**: FluentAssertions 7.x (fluent assertion library for expressive tests)
- **Mocking**: NSubstitute 5.x (preferred for .NET 10) - simpler, more intuitive than Moq
- **Integration Testing**: WebApplicationFactory (ASP.NET Core in-memory testing), Testcontainers.NET 4.x
- **Database Testing**: Testcontainers.CosmosDb for isolated test databases, Respawn for cleanup
- **Snapshot Testing**: Verify.Xunit for API response verification
- **Code Coverage**: Coverlet for code coverage collection

### Test Project Configuration

All test projects follow .NET 10 best practices with consistent package references:

**Example Test Project (.csproj)**:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="Testcontainers" Version="4.1.0" />
    <PackageReference Include="Testcontainers.CosmosDb" Version="4.1.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\LibrisMaleficarum.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
    <Using Include="NSubstitute" />
  </ItemGroup>

</Project>
```

**Key Configuration Features**:

- **Nullable Reference Types**: Enabled for better null safety
- **Implicit Usings**: Enabled for cleaner code (global usings for common namespaces)
- **Global Usings**: xUnit, FluentAssertions, NSubstitute automatically available
- **IsTestProject**: Marks project as test project for proper tooling support
- **Coverlet**: Code coverage collector for CI/CD integration

### Test Organization

```text
libris-maleficarum-service/
├── src/
│   ├── Api/
│   ├── Domain/
│   ├── Infrastructure/
│   └── Orchestration/
└── tests/
    ├── Api.Tests/                      # API endpoint tests
    │   ├── Controllers/
    │   ├── Integration/
    │   └── WebApplicationFactoryFixture.cs
    ├── Domain.Tests/                   # Domain logic tests
    │   ├── Entities/
    │   └── ValueObjects/
    ├── Infrastructure.Tests/           # Repository, EF Core tests
    │   ├── Repositories/
    │   └── CosmosDbFixture.cs
    └── Orchestration.Tests/            # Aspire orchestration tests
```

### Testing Patterns

**Unit Tests** (Domain Layer):

```csharp
public class WorldEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var worldId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();
        
        // Act
        var entity = WorldEntity.Create(worldId, "Test World", ownerId);
        
        // Assert
        entity.Should().NotBeNull();
        entity.WorldId.Should().Be(worldId);
        entity.OwnerId.Should().Be(ownerId);
        entity.IsDeleted.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string invalidName)
    {
        // Arrange & Act
        var act = () => WorldEntity.Create(Guid.NewGuid().ToString(), invalidName, Guid.NewGuid().ToString());
        
        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
```

**Integration Tests** (API Layer with WebApplicationFactory):

```csharp
public class WorldEntityControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public WorldEntityControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetWorld_WithValidId_ReturnsWorld()
    {
        // Arrange
        var worldId = await CreateTestWorld();
        
        // Act
        var response = await _client.GetAsync($"/api/v1/worlds/{worldId}");
        
        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var world = await response.Content.ReadFromJsonAsync<WorldEntity>();
        world.Should().NotBeNull();
        world!.Id.Should().Be(worldId);
        world.WorldId.Should().Be(worldId);
    }
    
    [Fact]
    public async Task CreateWorld_WithValidData_Returns201Created()
    {
        // Arrange
        var request = new CreateWorldRequest 
        { 
            Name = "New World", 
            OwnerId = "user-123" 
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/worlds", request);
        
        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        // Verify created resource
        var world = await response.Content.ReadFromJsonAsync<WorldEntity>();
        world.Should().NotBeNull();
        world!.Name.Should().Be(request.Name);
        world.OwnerId.Should().Be(request.OwnerId);
    }
    
    [Fact]
    public async Task CreateWorld_WithInvalidData_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateWorldRequest 
        { 
            Name = "", // Invalid: empty name
            OwnerId = "user-123" 
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/worlds", request);
        
        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }
}
```

**Repository Tests** (with Testcontainers for Cosmos DB):

```csharp
public class WorldEntityRepositoryTests : IClassFixture<CosmosDbFixture>
{
    private readonly CosmosDbFixture _fixture;
    private readonly IWorldEntityRepository _repository;
    
    public WorldEntityRepositoryTests(CosmosDbFixture fixture)
    {
        _fixture = fixture;
        _repository = new WorldEntityRepository(fixture.Container);
    }
    
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsEntity()
    {
        // Arrange
        var entity = await _fixture.SeedWorldEntity();
        
        // Act
        var result = await _repository.GetByIdAsync(entity.WorldId, entity.Id);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
    }
    
    [Fact]
    public async Task CreateAsync_WithValidEntity_Succeeds()
    {
        // Arrange
        var entity = WorldEntityBuilder.Default().Build();
        
        // Act
        await _repository.CreateAsync(entity);
        
        // Assert - verify entity exists
        var saved = await _repository.GetByIdAsync(entity.WorldId, entity.Id);
        saved.Should().BeEquivalentTo(entity);
    }
}
```

**Aspire Orchestration Tests**:

```csharp
// TBC - Aspire testing patterns to be defined once backend is implemented
// Will include:
// - Service dependency testing
// - Container orchestration validation
// - Configuration testing
```

### Test Data Builders

```csharp
public class WorldEntityBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _worldId = Guid.NewGuid().ToString();
    private string _name = "Test World";
    private string _ownerId = "test-user";
    
    public static WorldEntityBuilder Default() => new();
    
    public WorldEntityBuilder WithId(string id)
    {
        _id = id;
        return this;
    }
    
    public WorldEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public WorldEntity Build()
    {
        return WorldEntity.Create(_worldId, _name, _ownerId) with { Id = _id };
    }
}
```

### Cosmos DB Testing Strategy

1. **Local Development**: Use Cosmos DB Emulator
2. **CI Pipeline**: Use Testcontainers.CosmosDb for isolated test databases
3. **Integration Tests**: Create dedicated containers per test class, cleanup with Respawn
4. **Partition Key Testing**: Validate hierarchical partition key queries return correct RU costs

### Test Execution

```bash
dotnet test                                    # Run all tests
dotnet test --filter Category=Unit            # Unit tests only
dotnet test --filter Category=Integration     # Integration tests only
dotnet test --collect:"XPlat Code Coverage"   # With coverage
```

### Coverage Targets

- **Domain Layer**: 90%+ (business logic critical)
- **API Layer**: 80%+
- **Infrastructure Layer**: 70%+ (external dependencies harder to test)
- **Overall**: 80%+ minimum

## General Testing Strategies

### Test Pyramid

- **70% Unit Tests**: Fast, isolated, test business logic
- **20% Integration Tests**: Test component interactions, database access
- **10% E2E Tests**: Test critical user workflows end-to-end

### Test Patterns

- **AAA Pattern**: Arrange-Act-Assert for clarity
- **Test Data Builders**: Fluent builders for complex test data
- **Fixture Pattern**: Shared setup/teardown with IClassFixture
- **Theory Tests**: Parameterized tests with InlineData/MemberData

### Continuous Integration

- All tests run on every pull request (GitHub Actions)
- Code coverage reports published to PR comments
- Coverage gates enforce minimum thresholds
- Integration tests run in isolated containers
- E2E tests run on staging deployments

### Performance Testing

- **RU Cost Validation**: Assert Cosmos DB query costs match documented estimates (DATA_MODEL.md)
- **Load Testing**: Use NBomber or k6 for API load tests
- **Benchmarking**: BenchmarkDotNet for performance-critical code paths

## Test Organization Best Practices

- **One test class per production class** (unit tests)
- **Descriptive test names**: `MethodName_Scenario_ExpectedResult`
- **Arrange-Act-Assert comments** for complex tests
- **No test interdependencies**: Each test runs independently
- **Deterministic tests**: No reliance on system time, random data, or external state
