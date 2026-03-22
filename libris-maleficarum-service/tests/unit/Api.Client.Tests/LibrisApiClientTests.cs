namespace LibrisMaleficarum.Api.Client.Tests;

using System.Net;
using System.Text;
using System.Text.Json;

using LibrisMaleficarum.Api.Client.Exceptions;
using LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Unit tests for <see cref="LibrisApiClient"/>.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class LibrisApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Guid _worldId = Guid.NewGuid();

    [TestMethod]
    public async Task CreateWorldAsync_Success_ReturnsWorldResponse()
    {
        // Arrange
        var expectedWorld = new WorldResponse
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test World",
            Description = "A test world",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        var envelope = new ApiResponse<WorldResponse> { Data = expectedWorld };
        var client = CreateClient(HttpStatusCode.Created, envelope);

        var request = new CreateWorldRequest
        {
            Name = "Test World",
            Description = "A test world"
        };

        // Act
        var result = await client.CreateWorldAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedWorld.Id);
        result.OwnerId.Should().Be(expectedWorld.OwnerId);
        result.Name.Should().Be(expectedWorld.Name);
        result.Description.Should().Be(expectedWorld.Description);
    }

    [TestMethod]
    public async Task CreateEntityAsync_Success_ReturnsEntityResponse()
    {
        // Arrange
        var expectedEntity = new EntityResponse
        {
            Id = Guid.NewGuid(),
            WorldId = _worldId,
            ParentId = null,
            EntityType = "Character",
            Name = "Test Entity",
            Description = "A test entity",
            Tags = ["rpg", "npc"],
            Path = [],
            Depth = 0,
            HasChildren = false,
            OwnerId = Guid.NewGuid().ToString(),
            Attributes = new Dictionary<string, object> { ["level"] = 5 },
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            IsDeleted = false,
            SchemaVersion = 1
        };

        var envelope = new ApiResponse<EntityResponse> { Data = expectedEntity };
        var client = CreateClient(HttpStatusCode.Created, envelope);

        var request = new CreateEntityRequest
        {
            Name = "Test Entity",
            Description = "A test entity",
            EntityType = "Character",
            Tags = ["rpg", "npc"],
            Attributes = new Dictionary<string, object> { ["level"] = 5 },
            SchemaVersion = 1
        };

        // Act
        var result = await client.CreateEntityAsync(_worldId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedEntity.Id);
        result.WorldId.Should().Be(_worldId);
        result.EntityType.Should().Be("Character");
        result.Name.Should().Be("Test Entity");
        result.Tags.Should().BeEquivalentTo(["rpg", "npc"]);
    }

    [TestMethod]
    public async Task CreateWorldAsync_BadRequest_ThrowsLibrisApiException()
    {
        // Arrange
        var apiError = new ApiError
        {
            Message = "Validation failed",
            Code = "VALIDATION_ERROR",
            Details = new Dictionary<string, object?> { ["reason"] = "Name is required." }
        };

        var client = CreateClient(HttpStatusCode.BadRequest, new ApiErrorResponse { Error = apiError });
        var request = new CreateWorldRequest { Name = "" };

        // Act
        var act = () => client.CreateWorldAsync(request);

        // Assert
        var exception = await act.Should().ThrowAsync<LibrisApiException>();
        exception.Which.StatusCode.Should().Be(400);
        exception.Which.ApiError.Should().NotBeNull();
        exception.Which.ApiError!.Code.Should().Be("VALIDATION_ERROR");
    }

    [TestMethod]
    public async Task CreateWorldAsync_Unauthorized_ThrowsLibrisApiAuthenticationException()
    {
        // Arrange
        var apiError = new ApiError
        {
            Message = "Unauthorized",
            Code = "UNAUTHORIZED"
        };

        var client = CreateClient(HttpStatusCode.Unauthorized, new ApiErrorResponse { Error = apiError });
        var request = new CreateWorldRequest { Name = "Test" };

        // Act
        var act = () => client.CreateWorldAsync(request);

        // Assert
        var exception = await act.Should().ThrowAsync<LibrisApiAuthenticationException>();
        exception.Which.StatusCode.Should().Be(401);
    }

    [TestMethod]
    public async Task CreateWorldAsync_Forbidden_ThrowsLibrisApiAuthenticationException()
    {
        // Arrange
        var apiError = new ApiError
        {
            Message = "Forbidden",
            Code = "FORBIDDEN"
        };

        var client = CreateClient(HttpStatusCode.Forbidden, new ApiErrorResponse { Error = apiError });
        var request = new CreateWorldRequest { Name = "Test" };

        // Act
        var act = () => client.CreateWorldAsync(request);

        // Assert
        var exception = await act.Should().ThrowAsync<LibrisApiAuthenticationException>();
        exception.Which.StatusCode.Should().Be(403);
    }

    [TestMethod]
    public async Task CreateEntityAsync_CancellationRequested_ThrowsTaskCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var client = CreateClient(HttpStatusCode.OK, new ApiResponse<EntityResponse>
        {
            Data = new EntityResponse
            {
                Id = Guid.NewGuid(),
                WorldId = _worldId,
                EntityType = "Character",
                Name = "Test",
                Tags = [],
                Path = [],
                Depth = 0,
                HasChildren = false,
                OwnerId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                IsDeleted = false,
                SchemaVersion = 1
            }
        });

        var request = new CreateEntityRequest
        {
            Name = "Test",
            EntityType = "Character"
        };

        // Act
        var act = () => client.CreateEntityAsync(_worldId, request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [TestMethod]
    public async Task CreateWorldAsync_FlatErrorPayload_IsStillSupported()
    {
        // Arrange
        var apiError = new ApiError
        {
            Message = "Validation failed",
            Code = "VALIDATION_ERROR"
        };

        var client = CreateClient(HttpStatusCode.BadRequest, apiError);

        // Act
        var act = () => client.CreateWorldAsync(new CreateWorldRequest { Name = "" });

        // Assert
        var exception = await act.Should().ThrowAsync<LibrisApiException>();
        exception.Which.ApiError.Should().NotBeNull();
        exception.Which.ApiError!.Code.Should().Be("VALIDATION_ERROR");
    }

    private static LibrisApiClient CreateClient<T>(HttpStatusCode statusCode, T responseBody)
    {
        var json = JsonSerializer.Serialize(responseBody, JsonOptions);
        var handler = new MockHttpMessageHandler(statusCode, json);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost/")
        };

        return new LibrisApiClient(httpClient);
    }

    /// <summary>
    /// Mock HTTP message handler for unit testing.
    /// </summary>
    private sealed class MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
