# Research: Backend REST API with Cosmos DB Integration

**Phase**: 0 - Research & Technology Decisions  
**Date**: 2026-01-03  
**Purpose**: Document technology choices, best practices, and resolve all NEEDS CLARIFICATION items from Technical Context

## Technology Decisions

### 1. Aspire.NET for Local Development Orchestration

**Decision**: Use Aspire.NET 13.1 for local development with Cosmos DB Emulator integration

**Rationale**:

- **Constitution Requirement**: Section V mandates Aspire.NET AppHost for backend orchestration
- **Single-Command Startup**: `dotnet run --project src/Orchestration/AppHost` starts API + Cosmos DB Emulator
- **Service Discovery**: Automatic connection string injection eliminates manual configuration
- **Observability**: Aspire Dashboard provides logs, traces, metrics during development
- **Production Separation**: Aspire is development-time only; Azure Container Apps handles production

**Implementation**:

```csharp
// AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB Emulator for local development
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator(); // Uses Cosmos DB Emulator container

// Add API service with Cosmos DB reference
var apiService = builder.AddProject<Projects.LibrisMaleficarum_Api>("api")
    .WithReference(cosmosDb);

// Add React frontend (existing)
var frontend = builder.AddViteApp("frontend", "../../../../libris-maleficarum-app", "dev")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
```

**References**:

- [Aspire.NET Overview](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview)
- [Azure Cosmos DB component](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-component)
- [Cosmos DB Emulator integration](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-emulator)

**Alternatives Considered**:

- Manual Cosmos DB Emulator setup - Rejected: Requires developers to manage emulator lifecycle, connection strings manually
- Docker Compose - Rejected: Constitution requires Aspire.NET for developer experience

---

### 2. Entity Framework Core with Cosmos DB Provider

**Decision**: Use EF Core 10 with Microsoft.EntityFrameworkCore.Cosmos provider

**Rationale**:

- **Constitution Requirement**: Section IV mandates EF Core with Cosmos DB provider
- **LINQ Support**: Familiar query syntax for developers (vs. Cosmos DB SDK raw SQL)
- **Change Tracking**: Automatic update detection and optimistic concurrency
- **Repository Abstraction**: Clean Architecture requires repository pattern - EF Core fits naturally
- **Type Safety**: Strongly-typed entities with compile-time validation

**Configuration**:

```csharp
// ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public DbSet<World> Worlds { get; set; }
    public DbSet<WorldEntity> WorldEntities { get; set; }
    public DbSet<Asset> Assets { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Worlds container with partition key /WorldId (actually /id for root)
        modelBuilder.Entity<World>()
            .ToContainer("Worlds")
            .HasPartitionKey(w => w.Id)
            .HasNoDiscriminator();
            
        // WorldEntities container with partition key /WorldId
        modelBuilder.Entity<WorldEntity>()
            .ToContainer("WorldEntities")
            .HasPartitionKey(e => e.WorldId)
            .HasNoDiscriminator();
            
        // Assets container with partition key /WorldId
        modelBuilder.Entity<Asset>()
            .ToContainer("Assets")
            .HasPartitionKey(a => a.WorldId)
            .HasNoDiscriminator();
    }
}
```

**Key Considerations**:

- **No Migrations**: Cosmos DB is schemaless; EF Core migrations not applicable
- **Partition Keys**: Critical for performance; all queries should include partition key
- **Item Size Limit**: 2MB max per document (spec constraint documented)
- **ETag for Concurrency**: EF Core automatically manages ETags for optimistic concurrency

**References**:

- [EF Core Cosmos DB Provider](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/)
- [Cosmos DB Provider Limitations](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/limitations)
- [Partition Keys in EF Core](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/modeling)

**Alternatives Considered**:

- Azure Cosmos DB SDK - Rejected: Lower-level API, more boilerplate, less LINQ support
- Dapper + Cosmos DB - Rejected: No ORM benefits, manual mapping required

---

### 3. ASP.NET Core Controller-Based API (vs. Minimal APIs)

**Decision**: Use Controller-based API with attribute routing for initial implementation

**Rationale**:

- **Familiarity**: Well-established pattern with extensive documentation and tooling
- **Testability**: Controllers are easily unit-tested with `ControllerBase` mocking
- **Attribute Routing**: Clear, declarative routing with `[Route]` and `[HttpGet/Post/Put/Delete]`
- **Swagger Integration**: Seamless OpenAPI generation with Swashbuckle
- **Validation**: Built-in model binding with `[FromBody]`, `[FromRoute]` attributes

**Example**:

```csharp
[ApiController]
[Route("api/v1/worlds")]
public class WorldsController : ControllerBase
{
    private readonly IWorldRepository _worldRepository;
    private readonly IUserContextService _userContext;
    
    [HttpGet]
    [ProducesResponseType<ApiResponse<List<WorldResponse>>>(200)]
    public async Task<IActionResult> GetWorlds()
    {
        var userId = _userContext.GetCurrentUserId();
        var worlds = await _worldRepository.GetAllByOwnerAsync(userId);
        return Ok(ApiResponse.Success(worlds.Select(WorldResponse.FromDomain)));
    }
    
    [HttpPost]
    [ProducesResponseType<ApiResponse<WorldResponse>>(201)]
    [ProducesResponseType<ErrorResponse>(400)]
    public async Task<IActionResult> CreateWorld([FromBody] CreateWorldRequest request)
    {
        var world = new World(Guid.NewGuid(), _userContext.GetCurrentUserId(), request.Name, request.Description);
        await _worldRepository.CreateAsync(world);
        return CreatedAtAction(nameof(GetWorld), new { worldId = world.Id }, ApiResponse.Success(WorldResponse.FromDomain(world)));
    }
}
```

**References**:

- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Controller action return types](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types)
- [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)

**Alternatives Considered**:

- Minimal APIs - Deferred: Simpler for small APIs, but less structure for complex validation/filtering needs
- GraphQL - Rejected: Spec requires RESTful API; GraphQL noted as future enhancement

---

### 4. FluentValidation for Request Validation

**Decision**: Use FluentValidation library for request model validation

**Rationale**:

- **Separation of Concerns**: Validation logic separate from request models (vs. Data Annotations)
- **Testability**: Validators are easily unit-tested in isolation
- **Composability**: Reusable validation rules across multiple request models
- **Rich Validation**: Complex rules, conditional validation, custom error messages
- **FR-004a Compliance**: Provides field-level validation errors as required by spec

**Implementation**:

```csharp
public class CreateWorldRequestValidator : AbstractValidator<CreateWorldRequest>
{
    public CreateWorldRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("World name is required")
            .MaximumLength(100).WithMessage("World name must not exceed 100 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");
    }
}
```

**Integration with ASP.NET Core**:

```csharp
// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateWorldRequestValidator>();
```

**References**:

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Integration](https://docs.fluentvalidation.net/en/latest/aspnet.html)

**Alternatives Considered**:

- Data Annotations - Rejected: Less expressive, harder to test, validation logic mixed with models
- Manual Validation - Rejected: Boilerplate code, inconsistent error format

---

### 5. Stubbed User Context for Authentication

**Decision**: Implement `IUserContextService` with stubbed implementation returning hardcoded test user ID

**Rationale**:

- **FR-009**: Spec explicitly defers production authentication to future phase
- **Testability**: Easily mockable interface for repository and controller tests
- **Future-Proof**: Interface abstraction allows swapping to Azure Entra ID without breaking code
- **Row-Level Security**: Repositories can enforce OwnerId filtering using stubbed user ID

**Implementation**:

```csharp
// Domain/Interfaces/Services/IUserContextService.cs
public interface IUserContextService
{
    Guid GetCurrentUserId();
    string GetCurrentUserEmail();
    bool IsAuthenticated();
}

// Infrastructure/Services/UserContextService.cs (stubbed)
public class UserContextService : IUserContextService
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    
    public Guid GetCurrentUserId() => TestUserId;
    public string GetCurrentUserEmail() => "test@example.com";
    public bool IsAuthenticated() => true;
}
```

**Future Migration Path**:

```csharp
// Future: Production implementation with Azure Entra ID
public class EntraIdUserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public Guid GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("oid")?.Value;
        return Guid.Parse(userId ?? throw new UnauthorizedAccessException());
    }
}
```

**References**:

- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Azure Entra ID Integration](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-web-app-aspnet-core-sign-in)

**Alternatives Considered**:

- No abstraction - Rejected: Would require refactoring all repositories and controllers when adding real auth
- Middleware-based - Rejected: Harder to test, less explicit dependency injection

---

### 6. Repository Pattern Implementation

**Decision**: Implement repository pattern with async methods, GUID-based entity IDs, and partition key awareness

**Rationale**:

- **FR-007**: Spec mandates repository pattern with interfaces in Domain layer
- **Clean Architecture**: Abstracts data access, allows in-memory testing
- **Partition Key Optimization**: Repositories include partition key in queries (e.g., `GetByIdAsync(Guid worldId, Guid entityId)`)
- **Async/Await**: All I/O operations are asynchronous for scalability

**Example**:

```csharp
// Domain/Interfaces/Repositories/IWorldRepository.cs
public interface IWorldRepository
{
    Task<World?> GetByIdAsync(Guid worldId, CancellationToken cancellationToken = default);
    Task<List<World>> GetAllByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<World> CreateAsync(World world, CancellationToken cancellationToken = default);
    Task<World> UpdateAsync(World world, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid worldId, CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories/WorldRepository.cs
public class WorldRepository : IWorldRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<World?> GetByIdAsync(Guid worldId, CancellationToken cancellationToken = default)
    {
        return await _context.Worlds
            .WithPartitionKey(worldId.ToString()) // Explicit partition key for performance
            .FirstOrDefaultAsync(w => w.Id == worldId, cancellationToken);
    }
    
    public async Task<List<World>> GetAllByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Worlds
            .Where(w => w.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
        // Note: Cross-partition query - acceptable for user-scoped queries
    }
}
```

**Key Design Decisions**:

- **No Generic Repository**: Avoid over-abstraction; each repository has domain-specific methods
- **Return Domain Entities**: Repositories return domain entities, not DTOs (mapping happens in API layer)
- **Cancellation Tokens**: All async methods accept `CancellationToken` for graceful cancellation
- **Null Return**: `GetByIdAsync` returns `null` instead of throwing for not-found cases

**References**:

- [Repository Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [EF Core Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)

**Alternatives Considered**:

- Generic Repository - Rejected: Leads to leaky abstraction, doesn't fit domain-specific operations like `GetAllByOwnerAsync`
- Direct DbContext Injection - Rejected: Violates Clean Architecture, harder to test

---

### 7. Cursor-Based Pagination Implementation

**Decision**: Use continuation token-based pagination with Base64-encoded cursor

**Rationale**:

- **FR-013**: Spec requires cursor-based pagination
- **Cosmos DB Native**: Continuation tokens are built into Cosmos DB SDK
- **Consistency**: Cursors remain valid even if data changes (unlike offset pagination)
- **Performance**: No COUNT(*) queries needed, scalable to large datasets

**Implementation**:

```csharp
public async Task<(List<WorldEntity> Items, string? NextCursor)> GetPagedAsync(
    Guid worldId, 
    int limit = 50, 
    string? cursor = null, 
    CancellationToken cancellationToken = default)
{
    var query = _context.WorldEntities
        .Where(e => e.WorldId == worldId)
        .OrderBy(e => e.Name);
    
    // Decode cursor if provided
    if (!string.IsNullOrEmpty(cursor))
    {
        var decodedCursor = DecodeCosmosDBContinuationToken(cursor);
        // EF Core Cosmos doesn't directly support continuation tokens in LINQ
        // Fallback: Use skip/take with encoded offset (simplified approach)
        var offset = int.Parse(decodedCursor);
        query = query.Skip(offset);
    }
    
    var items = await query.Take(limit + 1).ToListAsync(cancellationToken);
    
    var hasMore = items.Count > limit;
    var nextCursor = hasMore ? EncodeNextCursor(cursor, limit) : null;
    
    return (items.Take(limit).ToList(), nextCursor);
}
```

**Note**: EF Core Cosmos provider has limitations with native continuation tokens. Consider using Cosmos DB SDK directly for pagination in production (abstracted behind repository interface).

**References**:

- [Cosmos DB Pagination](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/pagination)
- [EF Core Paging](https://learn.microsoft.com/en-us/ef/core/querying/pagination)

**Alternatives Considered**:

- Offset/Limit Pagination - Rejected: Inefficient for large datasets, inconsistent results if data changes
- Keyset Pagination - Rejected: Requires composite keys, more complex than cursor-based

---

### 8. Error Handling and Consistent Response Format

**Decision**: Implement global exception handling middleware with standardized `ApiResponse<T>` and `ErrorResponse` wrappers

**Rationale**:

- **FR-005**: Spec requires consistent JSON response format with data + meta structure
- **FR-004a**: Field-level validation errors required
- **Centralized Logic**: Middleware handles all exceptions consistently
- **HTTP Status Codes**: FR-006 mandates appropriate status codes (200, 201, 400, 403, 404, 409, 500)

**Implementation**:

```csharp
// Api/Models/Responses/ApiResponse.cs
public class ApiResponse<T>
{
    public T Data { get; set; }
    public ResponseMeta Meta { get; set; }
    
    public static ApiResponse<T> Success(T data) => new()
    {
        Data = data,
        Meta = new ResponseMeta { RequestId = Guid.NewGuid().ToString(), Timestamp = DateTime.UtcNow }
    };
}

// Api/Models/Responses/ErrorResponse.cs
public class ErrorResponse
{
    public ErrorDetail Error { get; set; }
    public ResponseMeta Meta { get; set; }
}

public class ErrorDetail
{
    public string Code { get; set; }
    public string Message { get; set; }
    public List<ValidationError>? Details { get; set; } // Field-level validation errors
}

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
}

// Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (WorldNotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, 404, "WORLD_NOT_FOUND");
        }
        catch (UnauthorizedWorldAccessException ex)
        {
            await HandleExceptionAsync(context, ex, 403, "FORBIDDEN");
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, 500, "INTERNAL_SERVER_ERROR");
        }
    }
}
```

**References**:

- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Handle errors in ASP.NET Core web APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors)

**Alternatives Considered**:

- Exception Filters - Rejected: Less flexible than middleware, doesn't handle non-MVC exceptions
- Manual Try-Catch in Controllers - Rejected: Boilerplate code, inconsistent error format

---

### 9. Testing Strategy

**Decision**: Use xUnit with FluentAssertions, Moq, and AAA pattern across all test projects

**Rationale**:

- **Constitution III**: TDD principle mandates tests before implementation with AAA pattern
- **xUnit**: Modern .NET test framework with async support, dependency injection
- **FluentAssertions**: Readable assertions (`result.Should().Be(expected)`)
- **Moq**: Interface mocking for repository and service dependencies
- **WebApplicationFactory**: Integration tests with in-memory test server

**Test Structure (AAA Pattern)**:

```csharp
[Fact]
public async Task GetWorlds_WithExistingWorlds_ReturnsAllWorldsForUser()
{
    // Arrange
    var userId = Guid.NewGuid();
    var expectedWorlds = new List<World>
    {
        new World(Guid.NewGuid(), userId, "World 1", "Description 1"),
        new World(Guid.NewGuid(), userId, "World 2", "Description 2")
    };
    
    var mockRepository = new Mock<IWorldRepository>();
    mockRepository.Setup(r => r.GetAllByOwnerAsync(userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedWorlds);
    
    var controller = new WorldsController(mockRepository.Object, MockUserContext(userId));
    
    // Act
    var result = await controller.GetWorlds();
    
    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = (OkObjectResult)result;
    var response = okResult.Value.Should().BeOfType<ApiResponse<List<WorldResponse>>>().Subject;
    response.Data.Should().HaveCount(2);
    response.Data[0].Name.Should().Be("World 1");
}
```

**Test Coverage Targets**:

- **Domain**: 90%+ coverage (entities, value objects, business logic)
- **Infrastructure**: 80%+ coverage (repositories with in-memory provider)
- **API**: 80%+ coverage (controllers, middleware, validation)

**Integration Test Example**:

```csharp
public class WorldsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task POST_Worlds_WithValidRequest_Returns201Created()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var request = new CreateWorldRequest { Name = "Test World", Description = "Test" };
        
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/worlds", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<WorldResponse>>();
        content.Data.Name.Should().Be("Test World");
    }
}
```

**References**:

- [Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [FluentAssertions Documentation](https://fluentassertions.com/)

**Alternatives Considered**:

- xUnit - Considered: Popular but MSTest.Sdk is Microsoft's modern recommended approach for .NET 10+
- NUnit - Considered: Mature but MSTest.Sdk provides better integration with Microsoft.Testing.Platform
- Moq - Rejected: Has known vulnerabilities, NSubstitute has cleaner syntax and is Microsoft-recommended

---

## Resolved Clarifications

All items marked "NEEDS CLARIFICATION" in Technical Context have been resolved:

✅ **Language/Version**: .NET 10 with C# 14  
✅ **Primary Dependencies**: ASP.NET Core Web API 10.0, Aspire.NET 13.1, EF Core 10 Cosmos provider, FluentValidation  
✅ **Storage**: Azure Cosmos DB (Emulator for local dev via Aspire)  
✅ **Testing**: MSTest.Sdk, FluentAssertions, NSubstitute, WebApplicationFactory  
✅ **Target Platform**: Azure Container Apps (production), Aspire Dashboard (local)  
✅ **Performance Goals**: <200ms p95, 100 worlds in <5s  
✅ **Constraints**: 2MB Cosmos item limit, 25MB asset limit (configurable), max 200 items/page  
✅ **Scale/Scope**: 10k+ entities per world with row-level security

## Next Steps

Proceed to **Phase 1**:

1. Create `data-model.md` with entity schemas and relationships
1. Create `contracts/` with OpenAPI specifications for API endpoints
1. Create `quickstart.md` with developer setup instructions
1. Update agent context with technology stack details
