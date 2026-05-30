namespace LibrisMaleficarum.Import.Tests.Services;

using LibrisMaleficarum.Api.Client;
using LibrisMaleficarum.Api.Client.Models;
using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Models;
using LibrisMaleficarum.Import.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class WorldImportServiceTests
{
    private IImportSourceReader _reader = null!;
    private IImportValidator _validator = null!;
    private ILibrisApiClient _apiClient = null!;
    private WorldImportService _service = null!;

    private static readonly Guid TestWorldId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _reader = Substitute.For<IImportSourceReader>();
        _validator = Substitute.For<IImportValidator>();
        _apiClient = Substitute.For<ILibrisApiClient>();
        _service = new WorldImportService(_reader, _validator, _apiClient);
    }

    [TestMethod]
    public async Task ImportAsync_Success_CallsCreateWorldThenEntities()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        var callOrder = new List<string>();

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add("CreateWorld");
                return CreateWorldResponse();
            });

        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add($"CreateEntity:{callInfo.ArgAt<CreateEntityRequest>(1).Name}");
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options);

        // Assert
        callOrder[0].Should().Be("CreateWorld");
        callOrder.Skip(1).Should().AllSatisfy(c => c.Should().StartWith("CreateEntity:"));
        await _apiClient.Received(1).CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(2).CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ImportAsync_AssignsNewGuids()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        var capturedRequests = new List<CreateEntityRequest>();
        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequests.Add(callInfo.ArgAt<CreateEntityRequest>(1));
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.WorldId.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task ImportAsync_MapsPropertiesToProperties()
    {
        // Arrange
        var properties = new Dictionary<string, object> { ["population"] = 50000, ["climate"] = "temperate" };
        var entityDef = new EntityImportDefinition
        {
            LocalId = "e1",
            EntityType = "City",
            Name = "Test City",
            Properties = properties
        };
        var content = CreateSourceContent(entities: [entityDef]);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        CreateEntityRequest? capturedRequest = null;
        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.ArgAt<CreateEntityRequest>(1);
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Properties.Should().ContainKey("population");
        capturedRequest.Properties.Should().ContainKey("climate");
    }

    [TestMethod]
    public async Task ImportAsync_SetsSchemaVersionToOne()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        var capturedRequests = new List<CreateEntityRequest>();
        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequests.Add(callInfo.ArgAt<CreateEntityRequest>(1));
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options);

        // Assert
        capturedRequests.Should().AllSatisfy(r => r.SchemaVersion.Should().Be(1));
    }

    [TestMethod]
    public async Task ImportAsync_FailedEntity_SkipsDescendants()
    {
        // Arrange — root succeeds, but child (parent of grandchild) fails
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
            new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" },
            new() { LocalId = "grandchild", EntityType = "Region", Name = "Grandchild", ParentLocalId = "child" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());

        var entityCallCount = 0;
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                entityCallCount++;
                var req = callInfo.ArgAt<CreateEntityRequest>(1);
                if (req.Name == "Child")
                {
                    throw new InvalidOperationException("Simulated API failure");
                }
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.TotalEntitiesFailed.Should().BeGreaterThanOrEqualTo(1);
        result.TotalEntitiesSkipped.Should().BeGreaterThanOrEqualTo(1);
        result.Errors.Should().Contain(e => e.LocalId == "child");
    }

    [TestMethod]
    public async Task ImportAsync_ReportsProgress()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var progressReports = new List<ImportProgress>();
        var progress = Substitute.For<IProgress<ImportProgress>>();
        progress.When(p => p.Report(Arg.Any<ImportProgress>()))
            .Do(callInfo => progressReports.Add(callInfo.ArgAt<ImportProgress>(0)));

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options, progress);

        // Assert
        progressReports.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task ImportAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalEntitiesCreated.Should().Be(2);
        result.TotalEntitiesFailed.Should().Be(0);
        result.TotalEntitiesSkipped.Should().Be(0);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.WorldId.Should().Be(TestWorldId);
    }

    [TestMethod]
    public async Task ImportAsync_ValidateOnly_DoesNotCallApi()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        var options = CreateOptions(validateOnly: true);

        // Act
        await _service.ImportAsync("/test", options);

        // Assert
        await _apiClient.DidNotReceive().CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ImportAsync_CancellationToken_StopsImport()
    {
        // Arrange
        var entities = Enumerable.Range(1, 10)
            .Select(i => new EntityImportDefinition { LocalId = $"e{i}", EntityType = "Continent", Name = $"Entity {i}" })
            .ToList();
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        using var cts = new CancellationTokenSource();

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());

        var entityCallCount = 0;
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                entityCallCount++;
                if (entityCallCount >= 3)
                {
                    cts.Cancel();
                }
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        var act = () => _service.ImportAsync("/test", options, cancellationToken: cts.Token);

        // Assert — should throw OperationCanceledException or complete early
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*cancel*");
    }

    [TestMethod]
    public async Task ValidateAsync_ReturnsValidationResult()
    {
        // Arrange
        var content = CreateSourceContent();
        var validationResult = new ImportValidationResult
        {
            Errors = [],
            Warnings = [],
            Manifest = CreateManifest(content)
        };

        _reader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(content);
        _validator.Validate(Arg.Any<ImportSourceContent>())
            .Returns(validationResult);

        // Act
        var result = await _service.ValidateAsync("/test");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Manifest.Should().NotBeNull();
        await _reader.Received(1).ReadAsync("/test", Arg.Any<CancellationToken>());
        _validator.Received(1).Validate(content);
    }

    [TestMethod]
    public async Task ImportAsync_EmptyEntityList_SucceedsWithZeroCounts()
    {
        // Arrange
        var content = CreateSourceContent(entities: []);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalEntitiesCreated.Should().Be(0);
        result.TotalEntitiesFailed.Should().Be(0);
        result.TotalEntitiesSkipped.Should().Be(0);
        result.WorldId.Should().Be(TestWorldId);
        await _apiClient.Received(1).CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ImportAsync_MultipleEntitiesAtSameDepth_CreatedInParallel()
    {
        // Arrange — three root-level entities at depth 0
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "r1", EntityType = "Continent", Name = "Root1" },
            new() { LocalId = "r2", EntityType = "Continent", Name = "Root2" },
            new() { LocalId = "r3", EntityType = "Continent", Name = "Root3" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var options = new ImportOptions
        {
            ApiBaseUrl = "https://test.api.local",
            AuthToken = "test-token-123",
            MaxConcurrency = 3
        };

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalEntitiesCreated.Should().Be(3);
        await _apiClient.Received(3).CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ImportAsync_EntityAlreadyInSkippedSet_IsSkipped()
    {
        // Arrange — root fails, so child at next depth should be skipped even though
        // the validator marked it valid. We simulate by making root throw.
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
            new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());

        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var req = callInfo.ArgAt<CreateEntityRequest>(1);
                if (req.Name == "Root")
                {
                    throw new InvalidOperationException("Root failure");
                }
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.TotalEntitiesFailed.Should().Be(1);
        result.TotalEntitiesSkipped.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.LocalId == "root");
        await _apiClient.Received(1).CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ImportAsync_MultipleDepths_ProcessesInOrder()
    {
        // Arrange — depth 0: root, depth 1: child, depth 2: grandchild
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
            new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" },
            new() { LocalId = "grandchild", EntityType = "City", Name = "Grandchild", ParentLocalId = "child" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        var callOrder = new List<string>();
        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add(callInfo.ArgAt<CreateEntityRequest>(1).Name);
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options);

        // Assert — depth 0 must be created before depth 1, which must be before depth 2
        callOrder.Should().ContainInOrder("Root", "Child", "Grandchild");
    }

    [TestMethod]
    public async Task ImportAsync_MissingDepthKey_ContinuesToNextDepth()
    {
        // Arrange — manifest has gaps in depth keys (e.g., depth 0 and 2, but not 1)
        var root = new EntityImportDefinition { LocalId = "root", EntityType = "Continent", Name = "Root" };
        var content = CreateSourceContent(entities: [root]);
        var manifest = CreateManifest(content);

        // Force a gap by removing depth 0 and adding a fake depth 2
        var hackedManifest = new ImportManifest
        {
            World = manifest.World,
            Entities = manifest.Entities,
            EntitiesByDepth = new Dictionary<int, IReadOnlyList<ResolvedEntity>>
            {
                [2] = manifest.EntitiesByDepth[0]
            },
            MaxDepth = 3,
            TotalEntityCount = manifest.TotalEntityCount,
            CountsByType = manifest.CountsByType
        };

        SetupMocks(content, hackedManifest);
        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert — should still succeed, processing only depth 2
        result.Success.Should().BeTrue();
        result.TotalEntitiesCreated.Should().Be(1);
    }

    [TestMethod]
    public async Task ImportAsync_ConcurrencyZero_UsesDefaultOfOne()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var options = new ImportOptions
        {
            ApiBaseUrl = "https://test.api.local",
            AuthToken = "test-token-123",
            MaxConcurrency = 0
        };

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.Success.Should().BeTrue();
        result.TotalEntitiesCreated.Should().Be(2);
    }

    [TestMethod]
    public async Task ImportAsync_NullProgress_DoesNotThrow()
    {
        // Arrange
        var content = CreateSourceContent();
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var options = CreateOptions();

        // Act
        var act = () => _service.ImportAsync("/test", options, progress: null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task ImportAsync_EntityWithNullParentId_CreatesAsRoot()
    {
        // Arrange — entity without ParentLocalId should have ParentId = null in request
        var entity = new EntityImportDefinition
        {
            LocalId = "orphan",
            EntityType = "Continent",
            Name = "Orphan"
        };
        var content = CreateSourceContent(entities: [entity]);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        CreateEntityRequest? capturedRequest = null;
        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.ArgAt<CreateEntityRequest>(1);
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.ParentId.Should().BeNull();
    }

    [TestMethod]
    public async Task ImportAsync_MultipleEntityTypes_TracksCountsByType()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "c1", EntityType = "Continent", Name = "Continent1" },
            new() { LocalId = "c2", EntityType = "Continent", Name = "Continent2" },
            new() { LocalId = "co1", EntityType = "Country", Name = "Country1" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.CreatedByType.Should().ContainKey("Continent").WhoseValue.Should().Be(2);
        result.CreatedByType.Should().ContainKey("Country").WhoseValue.Should().Be(1);
    }

    [TestMethod]
    public async Task ImportAsync_FailedEntity_HasCorrectErrorDetails()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
            new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" },
            new() { LocalId = "grandchild", EntityType = "City", Name = "Grandchild", ParentLocalId = "child" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());

        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var req = callInfo.ArgAt<CreateEntityRequest>(1);
                if (req.Name == "Child")
                {
                    throw new InvalidOperationException("Simulated API failure");
                }
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        var error = result.Errors.Should().ContainSingle().Subject;
        error.LocalId.Should().Be("child");
        error.EntityName.Should().Be("Child");
        error.ErrorMessage.Should().Be("Simulated API failure");
        error.SkippedDescendantLocalIds.Should().ContainSingle().Which.Should().Be("grandchild");
    }

    [TestMethod]
    public async Task ImportAsync_PartialFailure_SuccessIsFalse()
    {
        // Arrange — one entity fails, one succeeds
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "good", EntityType = "Continent", Name = "Good" },
            new() { LocalId = "bad", EntityType = "Continent", Name = "Bad" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());

        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var req = callInfo.ArgAt<CreateEntityRequest>(1);
                if (req.Name == "Bad")
                {
                    throw new InvalidOperationException("Failure");
                }
                return CreateEntityResponse();
            });

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.Success.Should().BeFalse();
        result.TotalEntitiesCreated.Should().Be(1);
        result.TotalEntitiesFailed.Should().Be(1);
    }

    [TestMethod]
    public async Task ImportAsync_ProgressReportsAllPhases()
    {
        // Arrange
        var entities = new List<EntityImportDefinition>
        {
            new() { LocalId = "e1", EntityType = "Continent", Name = "Entity1" }
        };
        var content = CreateSourceContent(entities: entities);
        var manifest = CreateManifest(content);
        SetupMocks(content, manifest);

        _apiClient.CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateWorldResponse());
        _apiClient.CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateEntityResponse());

        var progressReports = new List<ImportProgress>();
        var progress = Substitute.For<IProgress<ImportProgress>>();
        progress.When(p => p.Report(Arg.Any<ImportProgress>()))
            .Do(callInfo => progressReports.Add(callInfo.ArgAt<ImportProgress>(0)));

        var options = CreateOptions();

        // Act
        await _service.ImportAsync("/test", options, progress);

        // Assert
        progressReports.Should().Contain(p => p.Phase == ImportPhase.Reading);
        progressReports.Should().Contain(p => p.Phase == ImportPhase.Validating);
        progressReports.Should().Contain(p => p.Phase == ImportPhase.CreatingWorld);
        progressReports.Should().Contain(p => p.Phase == ImportPhase.CreatingEntities);
        progressReports.Should().Contain(p => p.Phase == ImportPhase.Complete);
    }

    [TestMethod]
    public async Task ImportAsync_ValidationFails_ReturnsErrorsWithoutCallingApi()
    {
        // Arrange
        var content = CreateSourceContent();
        var validationResult = new ImportValidationResult
        {
            Errors =
            [
                new ImportValidationError { Code = "TEST001", Message = "Test validation error", FilePath = "/test/entity.json" }
            ],
            Warnings = [],
            Manifest = null
        };

        _reader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(content);
        _validator.Validate(Arg.Any<ImportSourceContent>())
            .Returns(validationResult);

        var options = CreateOptions();

        // Act
        var result = await _service.ImportAsync("/test", options);

        // Assert
        result.Success.Should().BeFalse();
        result.WorldId.Should().Be(Guid.Empty);
        result.TotalEntitiesCreated.Should().Be(0);
        result.TotalEntitiesFailed.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "[TEST001] Test validation error");
        await _apiClient.DidNotReceive().CreateWorldAsync(Arg.Any<CreateWorldRequest>(), Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().CreateEntityAsync(Arg.Any<Guid>(), Arg.Any<CreateEntityRequest>(), Arg.Any<CancellationToken>());
    }

    #region Helper Methods

    private static ImportSourceContent CreateSourceContent(
        WorldImportDefinition? world = null,
        List<EntityImportDefinition>? entities = null)
    {
        return new ImportSourceContent
        {
            World = world ?? new WorldImportDefinition { Name = "Test World", Description = "A test world" },
            Entities = entities ?? new List<EntityImportDefinition>
            {
                new() { LocalId = "root", EntityType = "Continent", Name = "Root" },
                new() { LocalId = "child", EntityType = "Country", Name = "Child", ParentLocalId = "root" }
            },
            ParseErrors = [],
            SourcePath = "/test",
            SourceType = ImportSourceType.Folder
        };
    }

    private static ImportManifest CreateManifest(ImportSourceContent content)
    {
        var resolvedEntities = new List<ResolvedEntity>();
        var entitiesByDepth = new Dictionary<int, IReadOnlyList<ResolvedEntity>>();
        var idMap = new Dictionary<string, Guid>();
        var maxDepth = 0;

        // Assign GUIDs
        foreach (var entity in content.Entities)
        {
            idMap[entity.LocalId] = Guid.NewGuid();
        }

        // Compute depth for each entity
        int GetDepth(EntityImportDefinition e)
        {
            var depth = 0;
            var current = e;
            while (current?.ParentLocalId != null)
            {
                depth++;
                current = content.Entities.FirstOrDefault(x => x.LocalId == current.ParentLocalId);
            }
            return depth;
        }

        foreach (var entity in content.Entities)
        {
            var depth = GetDepth(entity);
            maxDepth = Math.Max(maxDepth, depth);

            var path = new List<Guid>();
            var current = entity;
            var ancestors = new List<Guid>();
            while (current?.ParentLocalId != null && idMap.ContainsKey(current.ParentLocalId))
            {
                ancestors.Add(idMap[current.ParentLocalId]);
                current = content.Entities.FirstOrDefault(x => x.LocalId == current.ParentLocalId);
            }
            ancestors.Reverse();
            path.AddRange(ancestors);

            var resolved = new ResolvedEntity
            {
                Definition = entity,
                AssignedId = idMap[entity.LocalId],
                ResolvedParentId = entity.ParentLocalId != null && idMap.TryGetValue(entity.ParentLocalId, out var pid) ? pid : null,
                Path = path,
                Depth = depth,
                Children = []
            };
            resolvedEntities.Add(resolved);
        }

        // Populate Children based on parent relationships
        var entityMap = resolvedEntities.ToDictionary(e => e.Definition.LocalId);
        foreach (var resolved in resolvedEntities)
        {
            if (resolved.Definition.ParentLocalId is not null &&
                entityMap.TryGetValue(resolved.Definition.ParentLocalId, out var parent))
            {
                parent.Children.Add(resolved);
            }
        }

        // Group by depth
        foreach (var group in resolvedEntities.GroupBy(e => e.Depth))
        {
            entitiesByDepth[group.Key] = group.ToList();
        }

        var countsByType = resolvedEntities
            .GroupBy(e => e.Definition.EntityType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ImportManifest
        {
            World = content.World ?? new WorldImportDefinition { Name = "Test World" },
            Entities = resolvedEntities,
            EntitiesByDepth = entitiesByDepth,
            MaxDepth = maxDepth,
            TotalEntityCount = resolvedEntities.Count,
            CountsByType = countsByType
        };
    }

    private void SetupMocks(ImportSourceContent content, ImportManifest manifest)
    {
        _reader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(content);

        var validationResult = new ImportValidationResult
        {
            Errors = [],
            Warnings = [],
            Manifest = manifest
        };
        _validator.Validate(Arg.Any<ImportSourceContent>())
            .Returns(validationResult);
    }

    private static ImportOptions CreateOptions(bool validateOnly = false)
    {
        return new ImportOptions
        {
            ApiBaseUrl = "https://test.api.local",
            AuthToken = "test-token-123",
            ValidateOnly = validateOnly
        };
    }

    private static WorldResponse CreateWorldResponse()
    {
        return new WorldResponse
        {
            Id = TestWorldId,
            OwnerId = Guid.NewGuid().ToString(),
            Name = "Test World",
            Description = "A test world",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private static EntityResponse CreateEntityResponse()
    {
        return new EntityResponse
        {
            Id = Guid.NewGuid(),
            WorldId = TestWorldId,
            EntityType = "Continent",
            Name = "Test Entity",
            Tags = [],
            Path = [],
            Depth = 0,
            HasChildren = false,
            OwnerId = Guid.NewGuid().ToString(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            IsDeleted = false,
            SchemaVersion = 1
        };
    }

    #endregion
}
