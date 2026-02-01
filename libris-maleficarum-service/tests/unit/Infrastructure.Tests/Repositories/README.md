# Repository Unit Tests

## Overview

This directory contains unit tests for repository implementations using the EF Core InMemory provider. The InMemory provider allows fast, in-process testing without external dependencies.

## InMemory Provider Limitations

The EF Core InMemory provider has several limitations compared to real database providers like Cosmos DB:

1. **No Collection Value Converters**: InMemory doesn't support converters for collection types (e.g., `List<Guid>` → `List<string>`)
2. **No Partition Key Support**: `.WithPartitionKey()` queries are ignored
3. **No TTL (Time-To-Live)**: Documents never auto-expire
4. **Different Performance**: Linear scans (O(n)) instead of indexed lookups (O(1))

## Testing Strategy

### ✅ Unit Tests (InMemory Provider)

**Tests Included:**
- `WorldRepositoryTests.cs` - World CRUD operations
- `WorldEntityRepositoryTests.cs` - Entity CRUD, hierarchy, authorization
- `AssetRepositoryTests.cs` - Asset management

**Tests Skipped:**
- ❌ **DeleteOperationRepository** - Deferred to integration tests only

### ✅ Integration Tests (Cosmos DB Emulator)

**Location:** `tests/integration/Api.IntegrationTests/`

**Tests Included:**
- `SoftDeleteIntegrationTests.cs` - Full end-to-end delete operation flow
  - Repository behavior (partition keys, value converters)
  - Service orchestration
  - API endpoint responses
  - Background processor execution

## DeleteOperationRepository Testing

### Why No Unit Tests?

The `DeleteOperationRepository` relies on Cosmos DB-specific features that cannot be accurately tested with InMemory:

1. **Value Converter for FailedEntityIds**:
   ```csharp
   // Cosmos DB: List<Guid> stored as List<string> via value converter
   // InMemory: ArgumentException - cannot compose converter
   Property(d => d.FailedEntityIds)
       .HasConversion(/* Guid → string converter */)
   ```

2. **Partition Key Queries**:
   ```csharp
   // Cosmos DB: O(1) lookup within partition
   // InMemory: Ignores partition key, scans all documents
   _context.Set<DeleteOperation>()
       .WithPartitionKey(worldId)
       .FirstOrDefaultAsync(d => d.Id == operationId)
   ```

3. **TTL Auto-Expiration**:
   ```csharp
   // Cosmos DB: Auto-deletes after 24 hours
   // InMemory: Never expires
   operation.Ttl = 86400; // 24 hours
   ```

### Solution: Conditional Configuration

The `DeleteOperationConfiguration` detects the database provider and applies features conditionally:

```csharp
// ApplicationDbContext.cs
var isCosmosDb = Database.IsCosmos();
modelBuilder.ApplyConfiguration(new DeleteOperationConfiguration(isCosmosDb));

// DeleteOperationConfiguration.cs
if (_isCosmosDb)
{
    // Apply value converter for Cosmos DB
    builder.Property(d => d.FailedEntityIds)
        .HasConversion(/* converter */)
        .ToJsonProperty("failedEntityIds");
}
else
{
    // Skip converter for InMemory (unit tests)
    builder.Property(d => d.FailedEntityIds)
        .ToJsonProperty("failedEntityIds");
}
```

This allows:
- ✅ InMemory provider to work without errors (100% unit test pass rate)
- ✅ Cosmos DB to use optimized storage and queries (production behavior)
- ✅ Integration tests to validate all production features

### Where to Find DeleteOperation Tests

**Full test coverage exists in integration tests:**

📂 **Location**: `tests/integration/Api.IntegrationTests/SoftDeleteIntegrationTests.cs`

**Test Cases**:
1. ✅ `DeleteEntity_WithValidEntity_Returns202AndCompletesSuccessfully` - End-to-end delete flow
2. ✅ `DeleteEntity_WithNonExistentEntity_Returns404` - Error handling
3. ✅ `DeleteEntity_WithUnauthorizedWorld_Returns403` - Authorization
4. ✅ `DeleteEntity_WithRateLimitExceeded_Returns429` - Rate limiting (repository query)
5. ✅ `DeleteEntity_WithIdempotentRequest_Returns202WithZeroCount` - Idempotency

**Integration Test Benefits**:
- Uses Cosmos DB emulator (real database behavior)
- Validates partition key queries
- Tests value converter serialization/deserialization
- Verifies TTL configuration (not expiration timing)
- Confirms discriminator filtering
- End-to-end API → Service → Repository → Database flow

## Adding New Repository Tests

### When to Use Unit Tests (InMemory)

✅ **Use InMemory unit tests when:**
- Repository uses standard EF Core features (no custom converters)
- No partition key queries required
- No TTL or Cosmos-specific features
- Fast feedback loop needed (< 1 second test execution)

### When to Use Integration Tests (Cosmos DB Emulator)

✅ **Use integration tests when:**
- Repository requires value converters
- Partition key queries are critical
- TTL behavior needs validation
- Cosmos DB-specific features (discriminators, indexing, etc.)
- End-to-end workflow validation needed

## Running Tests

### Unit Tests (Fast - InMemory)

```bash
# All unit tests
dotnet test --filter "TestCategory=Unit"

# Specific repository tests
dotnet test --filter "FullyQualifiedName~WorldEntityRepositoryTests"
```

**Expected Time**: ~5-10 seconds for all unit tests

### Integration Tests (Slower - Cosmos Emulator Required)

```bash
# All integration tests
dotnet test --filter "TestCategory=Integration"

# Specific integration tests
dotnet test --filter "TestCategory=Integration&FullyQualifiedName~SoftDeleteIntegrationTests"
```

**Prerequisites**:
- Azure Cosmos DB Emulator running locally
- AppHost orchestration (Aspire.NET)

**Expected Time**: ~30-60 seconds per integration test

## Best Practices

1. **Prefer Unit Tests**: Always try InMemory unit tests first for speed
2. **Use Integration Tests When Necessary**: Add integration tests for provider-specific features
3. **Document Trade-offs**: Explain why unit tests are skipped (like this README!)
4. **Test Both Paths**: Conditional configurations should be tested in both unit and integration tests
5. **Keep Tests Fast**: Use InMemory for quick feedback, Cosmos emulator for final validation

## References

- **Implementation Summary**: `specs/011-soft-delete-entities/IMPLEMENTATION_SUMMARY.md`
- **Conditional Configuration**: `src/Infrastructure/Persistence/Configurations/DeleteOperationConfiguration.cs`
- **Integration Tests**: `tests/integration/Api.IntegrationTests/SoftDeleteIntegrationTests.cs`
- **EF Core InMemory Limitations**: https://learn.microsoft.com/en-us/ef/core/providers/in-memory/limitations
