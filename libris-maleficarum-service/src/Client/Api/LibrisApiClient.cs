namespace LibrisMaleficarum.Api.Client;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using LibrisMaleficarum.Api.Client.Exceptions;
using LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// HTTP client implementation for interacting with the Libris Maleficarum API.
/// </summary>
public sealed class LibrisApiClient : ILibrisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrisApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API requests.</param>
    public LibrisApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<WorldResponse> CreateWorldAsync(
        CreateWorldRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(
            "api/v1/worlds",
            request,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return await HandleResponseAsync<WorldResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EntityResponse> CreateEntityAsync(
        Guid worldId,
        CreateEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/v1/worlds/{worldId}/entities",
            request,
            _jsonOptions,
            cancellationToken).ConfigureAwait(false);

        return await HandleResponseAsync<EntityResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> HandleResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await response.Content
                .ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (apiResponse is null)
            {
                throw new LibrisApiException(
                    (int)response.StatusCode,
                    "API returned a successful status code but the response body was empty or missing data.");
            }

            return apiResponse.Data;
        }

        var apiError = await TryReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
        var statusCode = (int)response.StatusCode;
        var errorMessage = apiError?.Message ?? $"API request failed with status code {statusCode}.";

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new LibrisApiAuthenticationException(statusCode, errorMessage, apiError);
        }

        throw new LibrisApiException(statusCode, errorMessage, apiError);
    }

    private async Task<ApiError?> TryReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiErrorResponse>(content, _jsonOptions);
            if (envelope?.Error is not null)
            {
                return envelope.Error;
            }
        }
        catch (JsonException)
        {
            // Fall back to legacy flat error payloads.
        }

        try
        {
            return JsonSerializer.Deserialize<ApiError>(content, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
