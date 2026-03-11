using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibrisMaleficarum.Import.Extensions;

/// <summary>
/// DI registration extensions for import services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers world import services in the DI container.
    /// </summary>
    public static IServiceCollection AddWorldImportServices(this IServiceCollection services)
    {
        services.AddScoped<IImportSourceReader, ImportSourceReader>();
        services.AddScoped<IImportValidator, ImportValidator>();
        services.AddScoped<IWorldImportService, WorldImportService>();
        return services;
    }
}
