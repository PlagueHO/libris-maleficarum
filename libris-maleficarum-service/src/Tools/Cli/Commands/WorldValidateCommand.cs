using System.CommandLine;
using LibrisMaleficarum.Api.Client.Extensions;
using LibrisMaleficarum.Cli.Output;
using LibrisMaleficarum.Import.Extensions;
using LibrisMaleficarum.Import.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LibrisMaleficarum.Cli.Commands;

/// <summary>
/// Validates import data without making API calls.
/// </summary>
public static class WorldValidateCommand
{
    /// <summary>
    /// Creates the "validate" subcommand with all options and handler.
    /// </summary>
    public static Command Create()
    {
        var sourceOption = new Option<string>("--source")
        {
            Description = "Path to import folder or zip file.",
            Required = true,
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Show detailed validation output.",
        };

        var command = new Command("validate", "Validate import data without making API calls");
        command.Add(sourceOption);
        command.Add(verboseOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var source = parseResult.GetRequiredValue(sourceOption);

            // Register API client with dummy URL since validate doesn't call the API
            var services = new ServiceCollection();
            services.AddLibrisApiClient(opt =>
            {
                opt.BaseUrl = "https://localhost";
            });
            services.AddWorldImportServices();

            await using var provider = services.BuildServiceProvider();
            var importService = provider.GetRequiredService<IWorldImportService>();

            var result = await importService.ValidateAsync(source, cancellationToken).ConfigureAwait(false);

            if (result.IsValid)
            {
                ConsoleReporter.ReportValidationSuccess(result);
                return 0;
            }

            ConsoleReporter.ReportValidationFailure(result);
            return 3;
        });

        return command;
    }
}
