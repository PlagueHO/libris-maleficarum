namespace LibrisMaleficarum.Api.Client.Extensions;

using System.Net.Http.Headers;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the Libris Maleficarum API client with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ILibrisApiClient"/> and its implementation with the service collection,
    /// configuring the underlying <see cref="HttpClient"/> using the provided options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure <see cref="LibrisApiClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLibrisApiClient(
        this IServiceCollection services,
        Action<LibrisApiClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new LibrisApiClientOptions { BaseUrl = string.Empty };
        configure(options);
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl must be configured.", nameof(options));
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new ArgumentException("BaseUrl must be a valid absolute URI.", nameof(options));
        }

        var builder = services.AddHttpClient<ILibrisApiClient, LibrisApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");

            if (!string.IsNullOrWhiteSpace(options.AuthToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", options.AuthToken);
            }
        });

        if (options.RequestTimeout.HasValue)
        {
            builder.AddStandardResilienceHandler(resilience =>
            {
                resilience.TotalRequestTimeout.Timeout = options.RequestTimeout.Value;
                resilience.AttemptTimeout.Timeout = options.RequestTimeout.Value;
            });
        }
        else
        {
            builder.AddStandardResilienceHandler();
        }

        return services;
    }
}
