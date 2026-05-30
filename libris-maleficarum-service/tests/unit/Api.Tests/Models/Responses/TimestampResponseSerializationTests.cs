namespace LibrisMaleficarum.Api.Tests.Models.Responses;

using System.Text.Json;
using FluentAssertions;
using LibrisMaleficarum.Api.Models.Responses;
using LibrisMaleficarum.Domain.ValueObjects;

[TestClass]
[TestCategory("Unit")]
public class TimestampResponseSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestMethod]
    public void WorldResponse_SerializesTimestampFields_AsCreatedAtAndUpdatedAt()
    {
        var response = new WorldResponse
        {
            Id = Guid.NewGuid(),
            OwnerId = "owner-1",
            Name = "Test World",
            Description = "desc",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("createdAt", out _).Should().BeTrue();
        root.TryGetProperty("updatedAt", out _).Should().BeTrue();
        root.TryGetProperty("createdDate", out _).Should().BeFalse();
        root.TryGetProperty("modifiedDate", out _).Should().BeFalse();
    }

    [TestMethod]
    public void EntityResponse_SerializesTimestampFields_AsCreatedAtAndUpdatedAt()
    {
        var response = new EntityResponse
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            ParentId = null,
            EntityType = EntityType.Continent,
            SchemaId = null,
            Name = "Test Entity",
            Description = "desc",
            Tags = [],
            Path = [],
            Depth = 0,
            HasChildren = false,
            OwnerId = "owner-1",
            CreatedBy = "owner-1",
            ModifiedBy = "owner-1",
            Properties = [],
            SystemProperties = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            DeletedDate = null,
            DeletedBy = null,
            SchemaVersion = 1
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("createdAt", out _).Should().BeTrue();
        root.TryGetProperty("updatedAt", out _).Should().BeTrue();
        root.TryGetProperty("createdDate", out _).Should().BeFalse();
        root.TryGetProperty("modifiedDate", out _).Should().BeFalse();
    }

    [TestMethod]
    public void AssetResponse_SerializesTimestampFields_AsCreatedAtAndUpdatedAt()
    {
        var response = new AssetResponse
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            FileName = "map.png",
            ContentType = "image/png",
            SizeBytes = 1024,
            BlobUrl = "https://example.blob.core.windows.net/assets/map.png",
            AssetType = LibrisMaleficarum.Domain.Entities.AssetType.Image,
            Tags = [],
            Description = "Map",
            ImageDimensions = new LibrisMaleficarum.Domain.Entities.ImageDimensions(1920, 1080),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("createdAt", out _).Should().BeTrue();
        root.TryGetProperty("updatedAt", out _).Should().BeTrue();
        root.TryGetProperty("createdDate", out _).Should().BeFalse();
        root.TryGetProperty("modifiedDate", out _).Should().BeFalse();
    }

    [TestMethod]
    public void SearchResultItem_SerializesTimestampFields_AsCreatedAtAndUpdatedAt()
    {
        var response = new SearchResultItem
        {
            Id = Guid.NewGuid(),
            Name = "Ironhold Castle",
            EntityType = "Building",
            DescriptionSnippet = "Ancient fortress",
            RelevanceScore = 0.95,
            WorldId = Guid.NewGuid(),
            ParentId = null,
            Tags = ["castle", "fortress"],
            OwnerId = "owner-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("createdAt", out _).Should().BeTrue();
        root.TryGetProperty("updatedAt", out _).Should().BeTrue();
        root.TryGetProperty("createdDate", out _).Should().BeFalse();
        root.TryGetProperty("modifiedDate", out _).Should().BeFalse();
    }
}
