using System.CommandLine;
using LibrisMaleficarum.Api.Client;
using LibrisMaleficarum.Api.Client.Extensions;
using LibrisMaleficarum.Cli.Output;
using LibrisMaleficarum.Import.Extensions;
using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Models;
using LibrisMaleficarum.Api.Client.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LibrisMaleficarum.Cli.Commands;

/// <summary>
/// Imports a world from a folder or zip archive into the backend API.
/// </summary>
public static class WorldImportCommand
{
    /// <summary>
    /// Creates the "import" subcommand with all options and handler.
    /// </summary>
    public static Command Create()
    {
        var sourceOption = new Option<string>("--source")
        {
            Description = "Path to import folder or zip file.",
            Required = true,
        };

        var apiUrlOption = new Option<string?>("--api-url")
        {
            Description = "Backend API base URL. Falls back to LIBRIS_API_URL env var.",
        };

        var tokenOption = new Option<string?>("--token")
        {
            Description = "Authentication token. Falls back to LIBRIS_API_TOKEN env var.",
        };

        var validateOnlyOption = new Option<bool>("--validate-only")
        {
            Description = "Validate import data without making API calls.",
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable detailed output during import.",
        };

        var maxConcurrencyOption = new Option<int>("--max-concurrency")
        {
            Description = "Max parallel API calls per hierarchy level.",
            DefaultValueFactory = _ => 10,
        };

        var logFileOption = new Option<string?>("--log-file")
        {
            Description = "Write detailed import log to file.",
        };

        var command = new Command("import", "Import a world from a folder or zip archive into the backend API");
        command.Add(sourceOption);
        command.Add(apiUrlOption);
        command.Add(tokenOption);
        command.Add(validateOnlyOption);
        command.Add(verboseOption);
        command.Add(maxConcurrencyOption);
        command.Add(logFileOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var source = parseResult.GetRequiredValue(sourceOption);
            var apiUrl = parseResult.GetValue(apiUrlOption);
            var token = parseResult.GetValue(tokenOption);
            var validateOnly = parseResult.GetValue(validateOnlyOption);
            var verbose = parseResult.GetValue(verboseOption);
            var maxConcurrency = parseResult.GetValue(maxConcurrencyOption);
            var logFile = parseResult.GetValue(logFileOption);

            // Set up DI container
            var services = new ServiceCollection();

            if (validateOnly)
            {
                services.AddSingleton<ILibrisApiClient, ValidationOnlyApiClient>();
            }
            else
            {
                // Resolve API URL: --api-url param > LIBRIS_API_URL env var
                apiUrl ??= Environment.GetEnvironmentVariable("LIBRIS_API_URL");
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    Console.Error.WriteLine("Error: API URL is required. Use --api-url or set the LIBRIS_API_URL environment variable.");
                    return 2;
                }

                // Resolve auth token: --token param > LIBRIS_API_TOKEN env var
                token ??= Environment.GetEnvironmentVariable("LIBRIS_API_TOKEN");
                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.Error.WriteLine("Error: Authentication token is required. Use --token or set the LIBRIS_API_TOKEN environment variable.");
                    return 2;
                }

                services.AddLibrisApiClient(opt =>
                {
                    opt.BaseUrl = apiUrl!;
                    opt.AuthToken = token;
                    opt.RequestTimeout = TimeSpan.FromMinutes(5);
                });
            }

            services.AddWorldImportServices();

            await using var provider = services.BuildServiceProvider();
            var importService = provider.GetRequiredService<IWorldImportService>();

            if (validateOnly)
            {
                var validationResult = await importService.ValidateAsync(source, cancellationToken).ConfigureAwait(false);

                if (validationResult.IsValid)
                {
                    ConsoleReporter.ReportValidationSuccess(validationResult);
                    return 0;
                }

                ConsoleReporter.ReportValidationFailure(validationResult);
                return 3;
            }

            // Build import options
            var options = new ImportOptions
            {
                ApiBaseUrl = apiUrl!,
                AuthToken = token!,
                MaxConcurrency = maxConcurrency,
                ValidateOnly = false,
                Verbose = verbose,
            };

            // Set up progress reporting
            var progress = new Progress<ImportProgress>(ConsoleReporter.ReportProgress);

            var result = await importService.ImportAsync(source, options, progress, cancellationToken).ConfigureAwait(false);

            // Write log file if specified
            if (!string.IsNullOrWhiteSpace(logFile))
            {
                await WriteLogFileAsync(logFile, result, cancellationToken).ConfigureAwait(false);
            }

            if (result.Success && result.TotalEntitiesFailed == 0)
            {
                ConsoleReporter.ReportImportSuccess(result);
                return 0;
            }

            if (result.TotalEntitiesCreated > 0)
            {
                ConsoleReporter.ReportImportPartialFailure(result);
                return 1;
            }

            ConsoleReporter.ReportImportTotalFailure(result);
            return 2;
        });

        return command;
    }

    private static async Task WriteLogFileAsync(
        string logFilePath,
        ImportResult result,
        CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var lines = new List<string>
        {
            $"Import Log - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"World ID:           {result.WorldId}",
            $"Duration:           {result.Duration.TotalSeconds:F1}s",
            $"Success:            {result.Success}",
            $"Entities created:   {result.TotalEntitiesCreated}",
            $"Entities failed:    {result.TotalEntitiesFailed}",
            $"Entities skipped:   {result.TotalEntitiesSkipped}",
            string.Empty,
            "Created by type:",
        };

        foreach (var (type, count) in result.CreatedByType.OrderBy(kvp => kvp.Key))
        {
            lines.Add($"  {type,-15} {count}");
        }

        if (result.Errors.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Errors:");

            foreach (var error in result.Errors)
            {
                var fileRef = error.FilePath is not null ? $"[{error.FilePath}] " : string.Empty;
                lines.Add($"  {fileRef}{error.EntityName}: {error.ErrorMessage}");

                if (error.SkippedDescendantLocalIds.Count > 0)
                {
                    lines.Add($"    Skipped descendants: {string.Join(", ", error.SkippedDescendantLocalIds)}");
                }
            }
        }

        await File.WriteAllLinesAsync(logFilePath, lines, cancellationToken).ConfigureAwait(false);
    }

    private sealed class ValidationOnlyApiClient : ILibrisApiClient
    {
        public Task<WorldResponse> CreateWorldAsync(
            CreateWorldRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The API client must not be used during validate-only execution.");
        }

        public Task<EntityResponse> CreateEntityAsync(
            Guid worldId,
            CreateEntityRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The API client must not be used during validate-only execution.");
        }
    }
}
