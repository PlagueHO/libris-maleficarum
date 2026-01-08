# PowerShell Integration Tests

End-to-end integration tests written in PowerShell/Pester that verify the World and Entity Management APIs work correctly against a running Aspire application.

## Overview

These tests use HTTP requests to validate the API endpoints exposed by the running Aspire application. Unlike the C# integration tests (which use `Aspire.Hosting.Testing` to manage the application lifecycle), these PowerShell tests expect the Aspire AppHost to already be running.

## Prerequisites

- **PowerShell 7.x**
- **Pester 5.x**: `Install-Module -Name Pester -Force -SkipPublisherCheck`
- **Aspire AppHost running** (see instructions below)

## Quick Start

### 1. Start the Aspire AppHost

The AppHost automatically manages all required services (Cosmos DB Emulator, API, Frontend):

```powershell
# From the service directory
cd libris-maleficarum-service

# Start AppHost (this starts Cosmos DB + API + Frontend)
dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj
```

**What happens:**

- ✅ Aspire starts the Cosmos DB Emulator in a container
- ✅ API service starts with proper connection string
- ✅ Frontend starts (Vite dev server)
- ✅ Aspire Dashboard opens at `https://localhost:15XXX` (URL shown in console)

**Wait for services to be ready** (30-60 seconds):
- Open the Aspire Dashboard (URL shown in startup logs)
- Verify all resources show "Running" status:
  - `cosmosdb` (green)
  - `api` (green)
  - `frontend` (green)

### 2. Run the Integration Tests

In a **new terminal** (keep AppHost running):

```powershell
# From the service directory
cd libris-maleficarum-service

# Run all integration tests
Invoke-Pester tests/Api.Tests/Integration/WorldAndEntityApiIntegration.Tests.ps1 -Output Detailed

# Run specific test suite
Invoke-Pester tests/Api.Tests/Integration/WorldAndEntityApiIntegration.Tests.ps1 -Output Detailed -Tag "Search"
```

### 3. Stop the AppHost

When finished, press `Ctrl+C` in the AppHost terminal to stop all services.

## Test Structure

### Test Suites

1. **World Management API Integration Tests** (`-Tag 'World'`)
   - Creating worlds
   - Listing worlds
   - Retrieving specific worlds by ID

2. **Entity Management API Integration Tests** (`-Tag 'Entity'`)
   - Creating top-level entities (continents)
   - Creating child entities with parent references
   - Retrieving all entities and children
   - Filtering by tags
   - Partial updates (PATCH)
   - Cascading deletes

3. **Entity Management Advanced Features** (`-Tag 'Advanced'`)
   - Filtering by entity type
   - Cursor-based pagination
   - Limit parameters

4. **Search and Filter API Integration Tests** (`-Tag 'Search'`)
   - Searching by name, description, tags
   - Case-insensitive search
   - Sorting results
   - Paginating search results

## API Details

### Base URL
```
https://localhost:7201/api/v1
```

This is the default HTTPS port assigned by Aspire to the API service. If you see a different port in the Aspire Dashboard, update `$script:BaseUrl` in the test file's `BeforeAll` block.

### Cosmos DB Emulator

The Cosmos DB Emulator is **automatically managed by Aspire**. You don't need to:
- ❌ Manually start Docker containers
- ❌ Set environment variables
- ❌ Configure connection strings

Aspire handles all of this for you.

**Emulator Details:**
- **Port**: Dynamically assigned by Aspire (typically 57XXX)
- **Account Key**: Standard Cosmos DB Emulator key (well-known, public)
- **SSL Certificate**: Auto-generated on first start (30-60s stabilization time)

## Troubleshooting

### ❌ "Unable to connect to the remote server"

**Cause**: API is not running or not ready.

**Solution**:
1. Verify AppHost is running (check terminal for "Distributed application started" message)
2. Open Aspire Dashboard (URL shown in AppHost logs)
3. Check that `api` resource shows "Running" status (green)
4. Wait 30-60 seconds for Cosmos DB SSL certificate to stabilize
5. Verify API port matches test expectations:
   ```powershell
   Test-NetConnection -ComputerName localhost -Port 7201
   ```

### ❌ SSL/TLS connection errors

**Cause**: Cosmos DB Emulator's self-signed SSL certificate is still being generated.

**Solution**:
- Wait longer (30-60 seconds after AppHost startup)
- Check Cosmos DB status in Aspire Dashboard
- The emulator needs time to generate its SSL certificate
- Tests include a 10-second wait in `BeforeAll`, but you may need to wait longer

### ❌ "404 Not Found" errors

**Cause**: API routes not registered or wrong base URL.

**Solution**:
1. Verify API is healthy in Aspire Dashboard
2. Check API port in Aspire Dashboard under `api` resource → Endpoints
3. Update `$script:BaseUrl` in the test file if port is different
4. Check API logs in Aspire Dashboard for startup errors

### ❌ Tests fail intermittently

**Cause**: Services not fully started or Cosmos DB still initializing.

**Solution**:
- Increase wait time in `BeforeAll` block (currently 10 seconds)
- Verify all services show "Running" in Aspire Dashboard before running tests
- Check Aspire Dashboard logs for any service errors

### ❌ AppHost fails to start

**Cause**: Various possibilities (port conflicts, Docker not running, etc.)

**Solution**:
1. Check Docker is running: `docker ps`
2. Check for port conflicts (ports 15XXX, 7201)
3. Review AppHost console output for specific error messages
4. Check Aspire Dashboard is not already running from a previous session

## CI/CD Integration

For CI/CD pipelines, consider:

1. **Start AppHost in background**:
   ```bash
   dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj &
   APP_PID=$!
   ```

2. **Wait for health**:
   ```bash
   timeout 60 bash -c 'until curl -k https://localhost:7201/health; do sleep 2; done'
   ```

3. **Run tests**:
   ```bash
   pwsh -Command "Invoke-Pester tests/Api.Tests/Integration/WorldAndEntityApiIntegration.Tests.ps1"
   ```

4. **Cleanup**:
   ```bash
   kill $APP_PID
   ```

Alternatively, use the C# integration tests (`Orchestration.Tests`) which automatically manage the AppHost lifecycle and are better suited for CI/CD.

## Comparison: PowerShell vs C# Integration Tests

| Aspect | PowerShell Tests | C# Tests (Orchestration.Tests) |
|--------|------------------|-------------------------------|
| **Purpose** | E2E HTTP API validation | AppHost + service integration |
| **Lifecycle** | Manual (AppHost must be running) | Automatic (tests start/stop AppHost) |
| **Best For** | Developer smoke testing | CI/CD automated testing |
| **Setup** | One command (start AppHost) | Zero setup (tests handle it) |
| **Speed** | Fast (services already running) | Slower (startup overhead per test) |
| **Isolation** | Shared state (manual cleanup) | Isolated (fresh start each test) |

**Recommendation**: Use C# tests for CI/CD and automated validation. Use PowerShell tests for quick manual verification during development.

## Test Data Cleanup

The tests create data in the Cosmos DB Emulator. To clean up:

**Option 1: Restart the AppHost**
```powershell
# Stop AppHost (Ctrl+C in its terminal)
# Start again (Cosmos DB container restarts with fresh data)
dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj
```

**Option 2: Use Cosmos DB Data Explorer**
1. Open Aspire Dashboard
2. Navigate to `cosmosdb` resource
3. Click "Data Explorer" link (if available)
4. Manually delete databases/containers

## Example: Running Tests

```powershell
# Terminal 1: Start AppHost
PS> cd libris-maleficarum-service
PS> dotnet run --project src/Orchestration/AppHost/LibrisMaleficarum.AppHost.csproj

# Output:
# info: Aspire.Hosting.DistributedApplication[0]
#       Now listening on: https://localhost:15269
# info: Aspire.Hosting.DistributedApplication[0]
#       Login to the dashboard at https://localhost:15269/login?t=...

# Terminal 2: Wait for services, then run tests
PS> cd libris-maleficarum-service
PS> Start-Sleep -Seconds 45  # Wait for Cosmos DB SSL
PS> Invoke-Pester tests/Api.Tests/Integration/WorldAndEntityApiIntegration.Tests.ps1 -Output Detailed

# Output:
# Starting discovery in 1 files.
# Discovery found 27 tests in 245ms.
# Running tests.
# [+] World Management API Integration Tests 1.23s (782ms|442ms)
# [+] Entity Management API Integration Tests 2.45s (1.2s|1.25s)
# Tests completed in 5.67s
# Tests Passed: 27, Failed: 0, Skipped: 0
```

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator)
- [Pester Documentation](https://pester.dev/)
- [C# Integration Tests](../../Orchestration.Tests/) - Automated tests using `Aspire.Hosting.Testing`
