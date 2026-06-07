<!-- markdownlint-disable-file -->
# User Settings Persistence — Backend Research

## Research Topics / Questions

Design a NEW persistence mechanism for per-user settings — **pinned World Entities (up to 5)** and **recent search items / recently opened entities** — currently planned for browser `localStorage`. Evaluate a new Cosmos DB "users" container (or alternatives) with backend APIs.

1. Cosmos container conventions (DbContext, EF config, partition keys, serialization, registration).
2. Repository pattern (interface + implementation, point-read vs query, soft-delete, RU).
3. Domain model conventions + propose `UserSettings` model.
4. User identity resolution across auth modes + partition key choice + anonymous-vs-Entra trade-off.
5. API conventions + propose REST contract.
6. Infra / provisioning (Bicep + Aspire AppHost) changes.
7. Alternative persistence options + recommendation.

**Status:** Complete.

---

## Critical Top-Line Findings

1. **Containers are NOT declared in Bicep or AppHost.** They are created at runtime by EF Core `ApplicationDbContext.Database.EnsureCreatedAsync()`. Adding a new container therefore requires **only** a new `DbSet`, a new `IEntityTypeConfiguration`, and `ApplyConfiguration` registration — **no Bicep change and no AppHost change**.
2. **Cosmos is provisioned as Serverless** (`EnableServerless` capability in `infra/main.bicep` line 462-464). There is **no manual/autoscale throughput** to configure — so `HasManualThroughput` is intentionally never used in this codebase.
3. **Anonymous mode is the default** and every request resolves to the single user id `_anonymous` (`UserContextService`). Per-user server persistence has near-zero benefit in anonymous single-user mode and only becomes meaningful in Entra ID multi-user mode. This is the central design trade-off (see Section 4).
4. The frontend currently only uses `localStorage` for **theme** (`libris-maleficarum-app/src/hooks/useTheme.ts`). Pinned/recent is a **planned, not-yet-implemented** feature, so there is no existing client contract to preserve.

---

## 1. Cosmos Container Conventions

### 1.1 DbContext

`libris-maleficarum-service/src/Infrastructure/Persistence/ApplicationDbContext.cs` lines 10-58:

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<World> Worlds { get; set; } = null!;
    public DbSet<WorldEntity> WorldEntities { get; set; } = null!;
    public DbSet<Asset> Assets { get; set; } = null!;
    public DbSet<DeleteOperation> DeleteOperations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Detect if using Cosmos DB or InMemory provider
        var isCosmosDb = Database.IsCosmos();

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new Configurations.WorldConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WorldEntityConfiguration(isCosmosDb));
        modelBuilder.ApplyConfiguration(new Configurations.AssetConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.DeleteOperationConfiguration(isCosmosDb));
    }
}
```

Pattern: one `DbSet<T>` per aggregate, one `IEntityTypeConfiguration<T>` per entity applied in `OnModelCreating`. `Database.IsCosmos()` (via `DbContextExtensions`) is passed to configurations that branch on provider for unit-test (InMemory) compatibility.

### 1.2 Entity configuration — exact pattern to copy

`libris-maleficarum-service/src/Infrastructure/Persistence/Configurations/WorldConfiguration.cs` lines 10-56 (this is the cleanest single-container, simple-partition-key template, closest to what `UserSettings` needs):

```csharp
public class WorldConfiguration : IEntityTypeConfiguration<World>
{
    public void Configure(EntityTypeBuilder<World> builder)
    {
        // Configure Cosmos DB container
        builder.ToContainer("Worlds");

        // Configure partition key
        builder.HasPartitionKey(w => w.Id);

        // Disable discriminator for single-entity container
        builder.HasNoDiscriminator();

        // Configure primary key
        builder.HasKey(w => w.Id);

        // Configure properties
        builder.Property(w => w.Id)
            .ToJsonProperty("id")
            .IsRequired();

        builder.Property(w => w.OwnerId)
            .ToJsonProperty("ownerId")
            .IsRequired();

        builder.Property(w => w.Name)
            .ToJsonProperty("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Description)
            .ToJsonProperty("description")
            .HasMaxLength(2000);

        builder.Property(w => w.CreatedAt)
            .ToJsonProperty("createdAt")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .ToJsonProperty("updatedAt")
            .IsRequired();

        builder.Property(w => w.IsDeleted)
            .ToJsonProperty("isDeleted")
            .IsRequired();
    }
}
```

Key conventions observed across `WorldConfiguration.cs`, `AssetConfiguration.cs`, `WorldEntityConfiguration.cs`:

- `builder.ToContainer("<PascalCaseContainerName>")` — container names are PascalCase plural (`Worlds`, `WorldEntities`, `Assets`).
- `builder.HasPartitionKey(...)` — simple key `/id` (World), simple `/worldId` (WorldEntity), hierarchical `[/worldId, /entityId]` (Asset, `AssetConfiguration.cs` line 23: `builder.HasPartitionKey(asset => new { asset.WorldId, asset.EntityId })`).
- `builder.HasNoDiscriminator()` when one entity type per container; `builder.HasDiscriminator<string>("_type").HasValue("WorldEntity")` when sharing a container (`WorldEntityConfiguration.cs` lines 41-42; `DeleteOperation` shares the `WorldEntities` container).
- Every persisted property uses `.ToJsonProperty("camelCaseName")` — this enforces the camelCase persistence contract from `docs/design/data_model.md`.
- **No `HasManualThroughput` / `HasAutoscaleThroughput` anywhere** — account is Serverless.
- Complex types (`Dictionary`, `List<Guid>`) use `.HasConversion(...)` + `.Metadata.SetValueComparer(...)` (see `WorldEntityConfiguration.cs` lines 84-160). For `UserSettings` we will need this for the pinned/recent collections.
- Owned value objects use `builder.OwnsOne(...)` (`AssetConfiguration.cs` lines 73-77).
- ETag optimistic concurrency: `builder.Property(asset => asset.ETag).IsETagConcurrency();` (`AssetConfiguration.cs` line 70).

### 1.3 JSON serialization (camelCase)

- **API layer:** `libris-maleficarum-service/src/Api/Program.cs` lines 190-197:
  ```csharp
  builder.Services.AddControllers(options =>
  ...
      options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  ```
- **Persistence layer:** camelCase is enforced per-property via `.ToJsonProperty(...)` in EF configs (above), not via a global serializer for EF Core. The standalone Worker path uses an explicit `SystemTextJsonCosmosSerializer` (`libris-maleficarum-service/src/Worker/SearchIndexWorker/SystemTextJsonCosmosSerializer.cs` lines 14-18) for raw Cosmos SDK access — relevant only if `UserSettings` ever needs a manual SDK path (it does not).
- `System.Text.Json` only; `Newtonsoft.Json` is banned (repo-wide rule).

### 1.4 DbContext registration (Aspire)

`libris-maleficarum-service/src/Api/Program.cs` lines 92-118:

```csharp
builder.AddCosmosDbContext<ApplicationDbContext>("cosmosdb", "LibrisMaleficarum",
    configureSettings: settings =>
    {
        if (builder.Environment.IsDevelopment())
        {
            settings.Credential = credential;
        }
        else
        {
            settings.Credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        }
    },
    configureDbContextOptions: options =>
        options.UseCosmos(cosmosOptions =>
        {
            // Gateway mode is required for the emulator (Direct mode is not supported)
            cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
            cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
        }));
```

`"cosmosdb"` = Aspire connection name (matches AppHost resource); `"LibrisMaleficarum"` = database name.

### 1.5 Container creation at runtime (NOT in infra)

`libris-maleficarum-service/src/Api/Program.cs` lines 316-349 — `EnsureCosmosDatabaseCreatedAsync` calls `context.Database.EnsureCreatedAsync(...)` with retry. AppHost (`AppHost.cs` lines 58-64) explicitly comments that "Container creation is owned by EF Core model configuration and happens during API startup via `ApplicationDbContext.Database.EnsureCreatedAsync()`". Bicep `sqlDatabases` is a placeholder (`infra/main.bicep` lines 491-499, container name `no-containers-specified`).

> **Conclusion:** Adding a `UserSettings` container = (1) add `DbSet<UserSettings>`, (2) add `UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>` with `.ToContainer("UserSettings")`, (3) register it in `OnModelCreating`. EF Core auto-creates the container on next startup.

---

## 2. Repository Pattern

### 2.1 Interface

`libris-maleficarum-service/src/Domain/Interfaces/Repositories/IWorldRepository.cs` lines 8-55 — interface lives in Domain, returns domain entities, all methods take `CancellationToken cancellationToken = default`, list methods return `(IEnumerable<T> Items, string? NextCursor)` tuples for cursor pagination.

### 2.2 Implementation — point read, soft delete, ownership

`libris-maleficarum-service/src/Infrastructure/Repositories/WorldRepository.cs`:

- Ctor injects `ApplicationDbContext`, `IUserContextService`, `ITelemetryService` (lines 25-33).
- **Point read with partition key** (lines 36-49):
  ```csharp
  var world = await _context.Worlds
      .WithPartitionKeyIfCosmos(_context, worldId)
      .FirstOrDefaultAsync(w => w.Id == worldId && !w.IsDeleted, cancellationToken);

  if (world is not null && world.OwnerId != currentUserId)
  {
      throw new UnauthorizedWorldAccessException(worldId, currentUserId);
  }
  ```
- **Ownership check** uses `_userContextService.GetCurrentUserIdAsync()` then compares `OwnerId` (lines 38, 44-47). Throws `UnauthorizedWorldAccessException`.
- **Soft delete** (lines 161-205): load entity → `world.SoftDelete()` (sets `IsDeleted = true`, updates `UpdatedAt`) → `SaveChangesAsync`.
- **ETag concurrency** (lines 144-154): reads `_etag` shadow property and compares.
- **Telemetry**: `_telemetryService.StartActivity(...)` wraps mutating operations.

### 2.3 Partition-key helper

`libris-maleficarum-service/src/Infrastructure/Extensions/DbSetExtensions.cs` lines 36-49:

```csharp
public static IQueryable<TEntity> WithPartitionKeyIfCosmos<TEntity>(
    this IQueryable<TEntity> source,
    ApplicationDbContext context,
    object partitionKey) where TEntity : class
{
    if (context.IsCosmos())
    {
        return source.WithPartitionKey(partitionKey);
    }
    return source; // InMemory unit tests: no-op
}
```

`UserSettings` repository should use `.WithPartitionKeyIfCosmos(_context, userId)` for its 1-RU point read.

### 2.4 DI registration

`libris-maleficarum-service/src/Api/Program.cs` lines 121-128:

```csharp
builder.Services.AddScoped<IUserContextService, UserContextService>();
...
builder.Services.AddScoped<IWorldRepository, WorldRepository>();
builder.Services.AddScoped<IWorldEntityRepository, WorldEntityRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
```

Add: `builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();`

### 2.5 RU considerations (from `docs/design/data_model.md` lines 60-90)

- Point read with partition key = **1 RU**. A `UserSettings` doc keyed by `/id` (= userId) gives a 1-RU point read for "load my settings".
- Cross-partition queries = 5-60 RUs (avoid). UserSettings never needs cross-partition reads because the caller always knows their own userId.
- Write/update = 5-12 RUs. Pinned/recent updates are infrequent (user-initiated) so cost is negligible.

---

## 3. Domain Model Conventions + Proposed `UserSettings`

### 3.1 Existing domain conventions

`libris-maleficarum-service/src/Domain/Entities/World.cs`:

- Class with `private set` properties and `private World()` ctor (lines 6-48).
- Static factory `World.Create(...)` (lines 60-73) sets `Id = Guid.NewGuid()`, `CreatedAt/UpdatedAt = DateTime.UtcNow`, `IsDeleted = false`, then `Validate()`.
- `Validate()` throws `ArgumentException` (lines 79-96).
- `Update(...)` mutates + bumps `UpdatedAt` (lines 103-110); `SoftDelete()` (lines 115-119).

Required system fields seen across `World` / `BaseWorldEntity` (`data_model.md` lines 130-195): `id`, `ownerId`/`OwnerId`, `createdAt`, `updatedAt`, `isDeleted` (+ optional `deletedDate`, `deletedBy`, `ttl`). Persisted as camelCase.

### 3.2 `docs/design/data_model.md` — user/settings coverage

Read in full (relevant lines): The doc defines **only three containers** (`World` `/id`, `WorldEntity` `/worldId`, `Asset` `[/worldId, /entityId]`) — table at lines 64-69. There is **no existing `User`, `UserSettings`, `preferences`, `pinned`, or `recent` concept** anywhere in the data model. The API doc (`docs/design/api.md` line 36-41) likewise mentions only World/WorldEntity/Asset/DeletedWorldEntity. The only "settings" reference in the codebase is the frontend settings modal mention in `docs/design/frontend.md` line 14. **This is greenfield** — adding `UserSettings` requires a new entry in `data_model.md` (Data-Shape Gate / persistence source-of-truth rule).

### 3.3 Proposed `UserSettings` domain model

Design decisions:

- **Partition key = `/id` where `id` = userId** (the oid claim, or `_anonymous`). Mirrors the `World` container's `/id` simple-key strategy and gives a guaranteed 1-RU point read. The userId is the natural, always-known key.
- **One document per user** (not per-world). Pinned and recent are scoped **per-world** as nested collections inside the single user doc, because the UX (`docs/design/frontend.md`) is "one World at a time" but pins/recents should persist when switching worlds. A single doc keeps reads/writes to 1 partition and avoids fan-out.
- **Denormalize minimal display data** (`worldId`, `entityId`, `name`, `entityType`) into pinned/recent items so the sidebar can render without N extra entity reads. Accept mild staleness (renamed/deleted entities) — refresh on click.
- Caps enforced in the domain: **pinned ≤ 5 per world**, **recent searches ≤ 10**, **recently opened ≤ 10** (ring-buffer semantics).

```csharp
namespace LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Per-user application settings: pinned entities, recent searches, and recently
/// opened entities. One document per user, partitioned by userId.
/// </summary>
public class UserSettings
{
    public const int MaxPinnedPerWorld = 5;
    public const int MaxRecentSearches = 10;
    public const int MaxRecentlyOpened = 10;

    /// <summary>Document id AND partition key — equals the user id (oid claim or "_anonymous").</summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>Pinned entities grouped per world (max 5 per world).</summary>
    public List<PinnedEntity> Pinned { get; private set; } = [];

    /// <summary>Recent free-text search queries (most-recent-first, max 10).</summary>
    public List<RecentSearch> RecentSearches { get; private set; } = [];

    /// <summary>Recently opened entities (most-recent-first, max 10).</summary>
    public List<RecentEntity> RecentlyOpened { get; private set; } = [];

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserSettings() { }

    public static UserSettings CreateDefault(string userId)
    {
        var now = DateTime.UtcNow;
        return new UserSettings
        {
            Id = userId,
            Pinned = [],
            RecentSearches = [],
            RecentlyOpened = [],
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Pin(PinnedEntity entity) { /* enforce MaxPinnedPerWorld, dedupe, bump UpdatedAt */ }
    public void Unpin(Guid worldId, Guid entityId) { /* remove, bump UpdatedAt */ }
    public void RecordSearch(RecentSearch search) { /* prepend, dedupe, trim to MaxRecentSearches */ }
    public void RecordOpened(RecentEntity entity) { /* prepend, dedupe, trim to MaxRecentlyOpened */ }
}

public record PinnedEntity
{
    public required Guid WorldId { get; init; }
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }       // denormalized display
    public required string EntityType { get; init; } // denormalized display
    public required DateTime PinnedAt { get; init; }
}

public record RecentSearch
{
    public required Guid WorldId { get; init; }       // searches are per-world (scoped to /worlds/{id}/search)
    public required string Query { get; init; }
    public required DateTime SearchedAt { get; init; }
}

public record RecentEntity
{
    public required Guid WorldId { get; init; }
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string EntityType { get; init; }
    public required DateTime OpenedAt { get; init; }
}
```

> Open design question for the user: should **recent searches** be per-world (proposed) or global-per-user? Search is invoked at `GET /api/v1/worlds/{worldId}/search` (`WorldsController.cs` line 115), so per-world is the natural scope.

### 3.4 Proposed EF config

```csharp
namespace LibrisMaleficarum.Infrastructure.Persistence.Configurations;

using LibrisMaleficarum.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

public sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    private readonly bool _isCosmosDb;

    public UserSettingsConfiguration(bool isCosmosDb = true) => _isCosmosDb = isCosmosDb;

    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToContainer("UserSettings");
        builder.HasPartitionKey(u => u.Id);
        builder.HasNoDiscriminator();
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ToJsonProperty("id").IsRequired();
        builder.Property(u => u.CreatedAt).ToJsonProperty("createdAt").IsRequired();
        builder.Property(u => u.UpdatedAt).ToJsonProperty("updatedAt").IsRequired();

        // Collections serialized as nested JSON via value converters (mirrors WorldEntity
        // properties bag pattern in WorldEntityConfiguration.cs lines 134-160).
        ConfigureJsonCollection(builder.Property(u => u.Pinned).ToJsonProperty("pinned"));
        ConfigureJsonCollection(builder.Property(u => u.RecentSearches).ToJsonProperty("recentSearches"));
        ConfigureJsonCollection(builder.Property(u => u.RecentlyOpened).ToJsonProperty("recentlyOpened"));
    }

    private static void ConfigureJsonCollection<T>(PropertyBuilder<List<T>> b)
    {
        b.HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<T>>(v, (JsonSerializerOptions?)null) ?? new List<T>())
         .Metadata.SetValueComparer(new ValueComparer<List<T>>(
            (l, r) => JsonSerializer.Serialize(l, (JsonSerializerOptions?)null)
                   == JsonSerializer.Serialize(r, (JsonSerializerOptions?)null),
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            v => v.ToList()));
    }
}
```

> Note: the existing `WorldEntityConfiguration` value-converter approach can stringify nested JSON (documented limitation in `data_model.md` lines 50-58). If pinned/recent must be real nested JSON arrays (e.g. for Cosmos queries), follow the same caveat — but UserSettings is **only ever point-read by id**, so stringified JSON inside the doc is acceptable and simpler. Flag for confirmation.

### 3.5 Proposed repository interface

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Repositories;

using LibrisMaleficarum.Domain.Entities;

public interface IUserSettingsRepository
{
    /// <summary>1-RU point read; returns null if the user has no settings document yet.</summary>
    Task<UserSettings?> GetAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Get-or-create: returns existing or a freshly persisted default document.</summary>
    Task<UserSettings> GetOrCreateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Upserts the full settings document for the user.</summary>
    Task<UserSettings> UpsertAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
```

Implementation mirrors `WorldRepository`: inject `ApplicationDbContext` + `IUserContextService` + `ITelemetryService`; point read via `.WithPartitionKeyIfCosmos(_context, userId).FirstOrDefaultAsync(u => u.Id == userId, ct)`; **ownership is implicit** (the document id IS the userId, so a user can only ever read their own partition — no `UnauthorizedWorldAccessException` needed, but the controller must always pass `GetCurrentUserIdAsync()`, never a client-supplied id).

---

## 4. User Identity & the Anonymous-vs-Entra Trade-off

### 4.1 How userId is resolved

`libris-maleficarum-service/src/Infrastructure/Services/UserContextService.cs` lines 14-33:

```csharp
public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    private const string AnonymousUserId = "_anonymous";

    public Task<string> GetCurrentUserIdAsync()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null)
            return Task.FromResult(AnonymousUserId);

        var oid = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                  ?? principal.FindFirst("oid")?.Value;

        return Task.FromResult(string.IsNullOrEmpty(oid) ? AnonymousUserId : oid);
    }
}
```

Per auth mode (`docs/design/authentication.md` lines 5-30, 60-78; `Program.cs` lines 43-56):

| Mode | How id resolves | Resulting userId |
| --- | --- | --- |
| **Anonymous** (default) | `AnonymousClaimsMiddleware` injects synthetic `oid = _anonymous` | `_anonymous` (one shared partition for everyone) |
| **Anonymous + Access Code** | Same synthetic claims; access code only gates entry, identity unchanged | `_anonymous` |
| **Entra ID** (multi-user) | `AddMicrosoftIdentityWebApiAuthentication`; real `oid` from JWT | the user's Entra object id (GUID) |

Mode is selected at startup: `var isMultiUserMode = !string.IsNullOrEmpty(azureAdClientId);` (`Program.cs` line 44).

### 4.2 Partition key / document id for UserSettings

Use the value from `GetCurrentUserIdAsync()` directly as both `id` and partition key:
- Anonymous → `id = "_anonymous"` (single doc shared by the single user — fine, since anonymous is single-user by definition).
- Entra → `id = "<oid-guid>"` (one doc per real user, isolated partitions).

This mirrors how `World.OwnerId` is already populated (`WorldRepository.CreateAsync` line 92: `World.Create(currentUserId, ...)`).

### 4.3 The trade-off (critical assessment)

- **In Anonymous / Access-Code mode**, server-side per-user settings provide **no multi-user value** — there is exactly one identity (`_anonymous`). The *only* benefit over `localStorage` is **cross-device / cross-browser sync** for that single user (e.g. pinning on a laptop and seeing it on a tablet pointed at the same instance). For a typical local/personal deployment this is marginal.
- **In Entra ID mode**, server-side settings are genuinely valuable: each user gets isolated, durable, cross-device pinned/recent state — which `localStorage` cannot provide.
- **Recommendation implication:** building a full Cosmos container + API that is only meaningful in the optional, "partially supported" Entra mode (`authentication.md` lines 18-20) is **premature** for the default deployment. This strongly favours the **hybrid** option (Section 7): keep `localStorage` as the baseline (works in every mode, zero backend cost), and add backend sync **conditionally** when Entra is configured.

---

## 5. API Conventions + Proposed Contract

### 5.1 Conventions

From `WorldsController.cs` and `docs/design/api.md`:

- Class: `[ApiController]` + `[Route("api/v1/<resource>")]` (`WorldsController.cs` lines 16-17).
- Versioning: `api/v1/...` path prefix (`api.md` lines 13, 66-72).
- Success envelope: `ApiResponse<T>` (`{ data, meta? }`, `ApiResponse.cs` lines 7-18) for single items; `PaginatedApiResponse<T>` (`{ data[], meta:{ count, nextCursor } }`, `PaginatedApiResponse.cs`) for lists.
- Error envelope: `ErrorResponse` → `{ error: { code, message, validationErrors?, details? }, meta? }` (`ErrorResponse.cs` lines 5-45). Codes are SCREAMING_SNAKE strings (`"WORLD_NOT_FOUND"`, `"VALIDATION_ERROR"`).
- `[ProducesResponseType(typeof(...), StatusCodes.Status2xx/4xx)]` on every action (`WorldsController.cs` lines 57-58, 267-270, 315-319).
- Validation: FluentValidation `IValidator<TRequest>` injected; on failure return `BadRequest(new ErrorResponse{...})` with `ValidationErrors` (`WorldsController.cs` lines 63-80).
- Identity: every action calls `await _userContextService.GetCurrentUserIdAsync()` (`WorldsController.cs` lines 82, 236) — never trusts a client-supplied user id.
- ETag: set `Response.Headers.ETag` and accept `If-Match` for concurrency (`WorldsController.cs` line 299).

### 5.2 Proposed `UserSettingsController` route table

Resource is the current user (`me`), mirroring REST convention and the existing `api/v1/<resource>` shape. Naming-discipline: reuse `api/v1` prefix verbatim; the new differing segment is `users/me/settings`.

| Method | Route | Purpose | Success type | Errors |
| --- | --- | --- | --- | --- |
| `GET` | `/api/v1/users/me/settings` | Get full settings (get-or-create default) | `ApiResponse<UserSettingsResponse>` | 401 |
| `PUT` | `/api/v1/users/me/settings` | Replace full settings document | `ApiResponse<UserSettingsResponse>` | 400, 401, 409 |
| `GET` | `/api/v1/users/me/pinned` | List pinned entities (optionally `?worldId=`) | `ApiResponse<IReadOnlyList<PinnedEntityResponse>>` | 401 |
| `PUT` | `/api/v1/users/me/pinned/{worldId:guid}/{entityId:guid}` | Pin an entity (idempotent; 409 if >5 for world) | `ApiResponse<PinnedEntityResponse>` | 400, 401, 409 |
| `DELETE` | `/api/v1/users/me/pinned/{worldId:guid}/{entityId:guid}` | Unpin an entity | 204 No Content | 401, 404 |
| `POST` | `/api/v1/users/me/recent-searches` | Record a recent search (`{ worldId, query }`) | `ApiResponse<RecentSearchResponse>` | 400, 401 |
| `GET` | `/api/v1/users/me/recent-searches` | List recent searches (optionally `?worldId=`) | `ApiResponse<IReadOnlyList<RecentSearchResponse>>` | 401 |
| `POST` | `/api/v1/users/me/recent-entities` | Record a recently opened entity | `ApiResponse<RecentEntityResponse>` | 400, 401 |
| `GET` | `/api/v1/users/me/recent-entities` | List recently opened entities | `ApiResponse<IReadOnlyList<RecentEntityResponse>>` | 401 |

Notes:
- `me` resolves server-side via `GetCurrentUserIdAsync()`; clients never send the id.
- `PUT .../pinned/{worldId}/{entityId}` mirrors the existing `{worldId:guid}/{entityId:guid}` route-constraint style from entity/asset routes (`api.md` lines 80-95).
- Recording endpoints are `POST` (append/ring-buffer, not idempotent replace).
- A `recent-searches` "pin/unpin"-style alternative naming was rejected to keep verbs RESTful and mirror existing `POST` create semantics.

---

## 6. Infra / Provisioning Changes

### 6.1 Bicep — NO change required

`infra/main.bicep` lines 445-503 — the Cosmos AVM module:

```bicep
module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.19.0' = {
  name: 'cosmos-db-account-deployment-${deploymentId}'
  scope: az.resourceGroup(effectiveResourceGroupName)
  dependsOn: [resourceGroup]
  params: {
    name: cosmosDbAccountName
    location: location
    tags: tags
    ...
    capabilitiesToAdd: [
      'EnableServerless'
    ]
    databaseAccountOfferType: 'Standard'
    ...
    disableLocalAuthentication: true
    networkRestrictions: {
      networkAclBypass: 'None'
      publicNetworkAccess: 'Disabled'
    }
    ...
    sqlDatabases: [
      {
        // EF Core creates application containers at runtime.
        // The 'leases' container (partition key: /id, 400 RU/s) is required by
        // the Cosmos DB Change Feed Processor for search index sync.
        // It is created automatically by the SearchIndexSyncService on first run.
        name: 'no-containers-specified'
      }
    ]
  }
}
```

Because `sqlDatabases` declares **no containers** (placeholder `no-containers-specified`) and EF Core owns container creation at runtime, **adding `UserSettings` needs no Bicep edit**. (If the team later decides to declare containers explicitly in Bicep for IaC completeness, the `UserSettings` container would be added to a `containers` array under the database with `paths: ['/id']` — but that is not the current pattern.)

### 6.2 Aspire AppHost — NO change required

`libris-maleficarum-service/src/Orchestration/AppHost/AppHost.cs` lines 48-64:

```csharp
#pragma warning disable ASPIRECOSMOSDB001 // Suppress experimental diagnostic for preview emulator
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithGatewayPort(cosmosDbGatewayPort);
        emulator.WithDataExplorer(cosmosDbDataExplorerPort);
    });

// Add the database to the Cosmos DB account
var cosmosDbDatabase = cosmosDb.AddCosmosDatabase("LibrisMaleficarum");

// Intentionally do not create application containers in AppHost.
// Container creation is owned by EF Core model configuration and happens during API startup
// via ApplicationDbContext.Database.EnsureCreatedAsync()...
#pragma warning restore ASPIRECOSMOSDB001
```

AppHost models only the **account** (`cosmosdb`) and **database** (`LibrisMaleficarum`); containers are NOT modeled here. The API already references the account via `.WithReference(cosmosDb)` (lines 130-145). **No AppHost change is needed** — the new container appears automatically when `EnsureCreatedAsync()` runs on API startup.

### 6.3 Net infra summary

The ENTIRE backend change is code-only in `libris-maleficarum-service`:
1. `src/Domain/Entities/UserSettings.cs` (+ `PinnedEntity`, `RecentSearch`, `RecentEntity`).
2. `src/Domain/Interfaces/Repositories/IUserSettingsRepository.cs`.
3. `src/Infrastructure/Persistence/Configurations/UserSettingsConfiguration.cs`.
4. `src/Infrastructure/Persistence/ApplicationDbContext.cs` — add `DbSet<UserSettings>` + `ApplyConfiguration`.
5. `src/Infrastructure/Repositories/UserSettingsRepository.cs`.
6. `src/Api/Controllers/UserSettingsController.cs` + request/response DTOs + FluentValidation validators.
7. `src/Api/Program.cs` — `AddScoped<IUserSettingsRepository, UserSettingsRepository>()`.
8. `docs/design/data_model.md` — add the `UserSettings` container row + schema (persistence source-of-truth rule).

No Bicep, no AppHost, no infra pipeline changes.

---

## 7. Alternative Persistence Options + Recommendation

| Option | RU / cost | Complexity | Cross-device | Anonymous value | Entra value | Fit |
| --- | --- | --- | --- | --- | --- | --- |
| **(a) New `UserSettings` Cosmos container** (partition `/id`=userId) | 1 RU read, 5-12 RU write; serverless = pay-per-op | Medium (new entity, config, repo, controller, validators, DTOs, data_model doc) | Yes | Low (single `_anonymous` doc) | High | Clean, mirrors `World`; but heavy for a feature only meaningful under optional Entra |
| **(b) Embed settings on an existing per-user doc / `World` doc** | 1 RU read but couples settings to world lifecycle | Medium-High | Yes | Low | Medium | Poor — no per-user doc exists; `World` is per-world not per-user, so pins spanning worlds don't fit; bloats world doc |
| **(c) Keep in browser `localStorage`** (current plan) | 0 backend cost | Low (frontend only) | **No** (device-local) | Adequate (single user, single device typical) | Insufficient (no per-user isolation across devices) | Best fit for default anonymous mode; matches existing `useTheme.ts` pattern |
| **(d) Hybrid: `localStorage`-first + optional backend sync for Entra users** | 0 in anonymous; 1 RU read / occasional write in Entra | Low now, Medium later (phase the backend in) | Yes (Entra only) | Adequate | High | **Best overall** — zero cost in the default mode, real value where it matters |

### Recommendation: **(d) Hybrid — `localStorage`-first, with backend sync gated on Entra ID**

Rationale grounded in this repo's reality:
1. **Anonymous is the default and the only fully-supported multi-mode** (`authentication.md` lines 16-20). In that mode a server-side per-user store adds cost and complexity for a single `_anonymous` partition whose only upside is cross-device sync — marginal for local/personal deployments.
2. **`localStorage` already matches the established frontend pattern** (`useTheme.ts`) and ships immediately with no backend, infra, or RU cost.
3. **Entra ID is where server persistence pays off.** Implement the Cosmos `UserSettings` container + API (Section 1-6) but **wire the frontend to call it only when `isMultiUserMode`/Entra is configured** (the same `azureAdClientId` switch used in `Program.cs` line 44 and propagated to the frontend via `ENTRA_CLIENT_ID` in `AppHost.cs` lines 148-160). In anonymous mode the client uses `localStorage`; in Entra mode it reads/writes the backend (optionally seeding from `localStorage` on first login).
4. **Cost of the hybrid is incremental and reversible** — the backend pieces are pure additive code (Section 6.3) with no infra churn, so they can land in a later phase without rework. Starting with `localStorage` does not paint us into a corner.

If the team's near-term target is explicitly **multi-user Entra deployments**, jump straight to option (a) (the container is cheap to add) and skip the `localStorage` baseline. Otherwise, (d) is the evidence-based choice.

---

## Recommended Next Research (not completed here)

- [ ] Confirm whether **recent searches** should be per-world or global-per-user (affects `RecentSearch.WorldId` requiredness and route query params).
- [ ] Decide whether pinned/recent collections must be **real nested JSON arrays** (queryable) vs **stringified JSON** inside the point-read doc — affects EF value-converter choice (`data_model.md` lines 50-58 caveat).
- [ ] Inspect the frontend `WorldSidebar` / `TopToolbar` components to define exact denormalized display fields the UI needs for pinned/recent rendering.
- [ ] Confirm TTL policy for `recent` items (auto-expire recents after N days via Cosmos `ttl`, mirroring `WorldEntity` 90-day TTL?).
- [ ] Verify FluentValidation validator-registration mechanism (assembly scan vs explicit `AddScoped`) used for new request DTOs (`Program.cs` line ~185 `SchemaVersionValidator` is explicit; check if there's `AddValidatorsFromAssembly`).

## Clarifying Questions for the User

1. Is the intended deployment primarily **anonymous single-user** (favours hybrid/localStorage) or **Entra multi-user** (favours building the container now)?
2. Should pinned/recent be **global per user** or **per world**? (Proposal: pinned per-world, recents per-world.)
3. Should recent items **auto-expire** (Cosmos TTL) or persist until evicted by the size cap?
4. Are the caps correct — **pinned 5/world, recent searches 10, recently opened 10**?
