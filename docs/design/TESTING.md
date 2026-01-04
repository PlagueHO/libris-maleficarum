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

- **Test Framework**: MSTest.Sdk 3.10.2+ (modern .NET 10 test SDK)
- **Test Runner**: Microsoft.Testing.Platform (built into MSTest.Sdk)
- **Assertions**: FluentAssertions 7.x
- **Mocking**: NSubstitute 5.x
- **Integration Testing**: Aspire.Hosting.Testing, Testcontainers.NET 4.x
- **Code Coverage**: Auto-included via MSTest.Sdk

### Test Project Configuration

**Domain/Infrastructure Tests (.csproj)**:

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />
    <Using Include="FluentAssertions" />
    <Using Include="NSubstitute" />
  </ItemGroup>
</Project>
```

**API Integration Tests (.csproj)**:

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" Version="13.1.0" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />
    <Using Include="FluentAssertions" />
    <Using Include="NSubstitute" />
    <Using Include="System.Net" />
    <Using Include="Aspire.Hosting.Testing" />
  </ItemGroup>
</Project>
```

**SDK Configuration (global.json)**:

```json
{
  "msbuild-sdks": {
    "MSTest.Sdk": "3.10.2"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

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
[TestClass]
public class WorldEntityTests
{
    [TestMethod]
    public void Create_WithValidData_ShouldSucceed()
    {
        var entity = WorldEntity.Create("world-1", "Test", "owner-1");
        
        entity.Should().NotBeNull();
        entity.WorldId.Should().Be("world-1");
    }
    
    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Create_WithInvalidName_ShouldThrow(string invalidName)
    {
        var act = () => WorldEntity.Create("world-1", invalidName, "owner-1");
        act.Should().Throw<ArgumentException>();
    }
}
```

**Integration Tests** (API Layer with Aspire):

```csharp
[TestClass]
public class ApiTests
{
    [TestMethod]
    public async Task GetEndpoint_ReturnsExpectedResponse()
    {
        await using var app = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.LibrisMaleficarum_AppHost>();
        
        await app.StartAsync();
        
        var httpClient = app.CreateHttpClient("apiservice");
        var response = await httpClient.GetAsync("/weatherforecast");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Repository Tests** (Infrastructure Layer):

```csharp
[TestClass]
public class WorldEntityRepositoryTests
{
    [TestMethod]
    public async Task CreateAsync_WithValidEntity_Succeeds()
    {
        // Use Testcontainers for isolated Cosmos DB
        var entity = new WorldEntity { Id = "1", Name = "Test" };
        
        await _repository.CreateAsync(entity);
        
        var saved = await _repository.GetByIdAsync("1");
        saved.Should().BeEquivalentTo(entity);
    }
}
```

### Test Execution

```bash
dotnet test                                                      # Run all tests
dotnet test --filter TestCategory!=Integration                  # Unit tests only
dotnet test --filter TestCategory=Integration                   # Integration tests only
dotnet test --coverage --coverage-output-format cobertura       # With coverage (Cobertura XML)
dotnet test --coverage --coverage-output-format xml             # With coverage (XML)
dotnet test --coverage --coverage-output-format coverage        # With coverage (binary)
```

### Coverage Targets

- **Domain Layer**: 90%+ (business logic critical)ClassInitialize/ClassCleanup and TestInitialize/TestCleanup
- **Data-Driven Tests**: Parameterized tests with DataTestMethod and DataRow attributes
- **Infrastructure Layer**: 70%+ (external dependencies harder to test)
- **Overall**: 80%+ minimum

## Best Practices

### Test Organization

- **Test Pyramid**: 70% unit, 20% integration, 10% E2E
- **Naming**: `MethodName_Scenario_ExpectedResult`
- **Pattern**: Arrange-Act-Assert
- **Attributes**: `[TestClass]`, `[TestMethod]`, `[DataTestMethod]`
- **Categories**: Use `[TestCategory("Unit")]` for filtering
- **Independence**: No shared state between tests

### MSTest Specifics

- **Class setup**: `[ClassInitialize]` / `[ClassCleanup]`
- **Test setup**: `[TestInitialize]` / `[TestCleanup]`
- **Data-driven**: `[DataTestMethod]` with `[DataRow(...)]`
- **FluentAssertions**: Use `.Should()` for readable assertions
- **NSubstitute**: Use `Substitute.For<T>()` for mocking

### Coverage with MSTest.Sdk

MSTest.Sdk 3.10.2+ includes built-in coverage via the Microsoft.Testing.Platform:

```bash
# Generate coverage in Cobertura format (recommended for CI/CD)
dotnet test --coverage --coverage-output-format cobertura

# Specify output location
dotnet test --coverage --coverage-output ./TestResults/coverage.cobertura.xml --coverage-output-format cobertura

# Multiple formats
dotnet test --coverage --coverage-output-format "cobertura,xml"

# Custom coverage settings
dotnet test --coverage --coverage-settings coverage.settings.xml
```

Coverage reports are generated in the `TestResults/` directory.

### Test Execution Examples

```bash
# Run all tests (coverage auto-collected)
dotnet test --filter TestCategory!=Integration   # Unit tests only
dotnet test --project tests/Api.Tests/           # Specific project
```

### Coverage Targets

- **Domain Layer**: 90%+ (business logic critical)
- **Infrastructure Layer**: 70%+ (external dependencies