# Performance Tests

This directory contains performance tests for the Libris Maleficarum backend API. These tests verify that the system meets performance requirements defined in the specification.

## Test Categories

All performance tests are marked with `[TestCategory("Performance")]` to allow selective execution.

### World Performance Tests (T156)

**File**: `WorldPerformanceTests.cs`

Tests verifying **SC-002** requirement: p95 response time <200ms for CRUD
operations.

- **CreateWorlds_100Requests_P95ResponseTimeLessThan200ms**: Creates 100
  worlds concurrently and measures p95 response time
- **GetWorlds_100Requests_P95ResponseTimeLessThan200ms**: Retrieves a world
  100 times concurrently and measures p95 response time

### Entity Performance Tests (T157)

**File**: `EntityPerformanceTests.cs`

Tests verifying pagination and filtering performance with large
datasets.

- **CreateAndPaginateEntities_1000Entities_PerformanceAcceptable**: Creates
  1000 entities in a single world and tests pagination with different page
  sizes (50, 100, 200)
- **FilterEntities_1000Entities_FilterPerformanceAcceptable**: Creates 1000
  entities with varied types/tags and tests filtering performance

## Running Performance Tests

### Run All Performance Tests

```powershell
dotnet test --filter "TestCategory=Performance" --project tests/performance/LibrisMaleficarum.Performance.Tests.csproj
```

### Run Specific Performance Test

```powershell
# World performance tests only
dotnet test --filter "FullyQualifiedName~WorldPerformanceTests" --project tests/performance/LibrisMaleficarum.Performance.Tests.csproj

# Entity performance tests only
dotnet test --filter "FullyQualifiedName~EntityPerformanceTests" --project tests/performance/LibrisMaleficarum.Performance.Tests.csproj

# Specific test method
dotnet test --filter "FullyQualifiedName~CreateWorlds_100Requests_P95ResponseTimeLessThan200ms" --project tests/performance/LibrisMaleficarum.Performance.Tests.csproj
```

### Run from Solution Root

```powershell
# All performance tests
dotnet test --filter "TestCategory=Performance" LibrisMaleficarum.slnx

# Verify performance tests are excluded by default
dotnet test LibrisMaleficarum.slnx
# (Performance tests should NOT run)
```

## Prerequisites

- **Aspire.NET**: Performance tests use the Aspire AppHost for orchestration
- **Cosmos DB Emulator**: Tests require Cosmos DB Emulator to be available
  (managed by Aspire)
- **Azurite**: Storage emulator for blob storage tests (managed by Aspire)

The tests will automatically start the AppHost and all required dependencies.

## Performance Metrics

### World Performance (T156 - SC-002)

**Requirement**: p95 response time <200ms for CRUD operations

**Measured**:

- Create World: p95 response time
- Get World: p95 response time
- Min, Average, Max response times for context

**Success Criteria**: p95 must be <200ms

### Entity Performance (T157)

**Creation Performance**:

- Total time to create 1000 entities
- Average creation time per entity

**Pagination Performance**:

- Response time for different page sizes (50, 100, 200)
- Number of pages required per page size
- Average time per page

**Filtering Performance**:

- Response time for type filtering
- Response time for tag filtering  
- Response time for combined filters

**Success Criteria**:

- All pagination requests complete in <500ms per page
- Filtering completes in <1000ms

## Test Output

Performance tests log detailed metrics to the console:

```text
Performance Metrics for 100 World Creations:
  Min Response Time: 45ms
  Average Response Time: 87.32ms
  P95 Response Time: 156ms
  Max Response Time: 234ms

Pagination Performance Results:
Page Size | Total Time | Page Count | Avg Time/Page
----------|------------|------------|---------------
       50 |      2340ms |         20 |        117.00ms
      100 |      1523ms |         10 |        152.30ms
      200 |       876ms |          5 |        175.20ms
```

## Notes

- **Concurrent Execution**: World creation tests execute requests concurrently to simulate realistic load
- **Sequential Pagination**: Pagination tests execute sequentially to
  measure actual cursor-based pagination performance
- **Cosmos DB Performance**: Performance depends on Cosmos DB provisioned
  RU/s (local emulator may differ from production)
- **Network Latency**: Tests measure full round-trip time including network
  latency
- **Warm-up**: First requests may be slower due to cold starts; tests
  account for this by creating multiple entities

## Troubleshooting

### Tests Fail with "Cannot connect to Cosmos DB"

Ensure Cosmos DB Emulator is running or Aspire AppHost has started successfully.

### Performance Tests Run During Regular Test Execution

Verify you're not using a filter that includes `TestCategory=Performance`.
Performance tests should only run when explicitly requested.

### P95 Times Exceed 200ms Threshold

- Check system resources (CPU, memory)
- Verify no other resource-intensive processes are running
- Consider that local emulator performance may differ from production
  Azure Cosmos DB
- Review Cosmos DB query patterns for optimization opportunities

## Future Enhancements

- Add performance tests for asset upload/download
- Add search performance tests with large datasets
- Add concurrency tests (multiple users accessing same world)
- Add load testing scenarios (sustained load over time)
- Add memory and CPU profiling
