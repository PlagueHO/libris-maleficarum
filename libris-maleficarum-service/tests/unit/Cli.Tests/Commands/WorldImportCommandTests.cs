using System.CommandLine;
using LibrisMaleficarum.Cli.Commands;

namespace LibrisMaleficarum.Cli.Tests.Commands;

[TestClass]
[TestCategory("Unit")]
public class WorldImportCommandTests
{
    private Command _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = WorldImportCommand.Create();
    }

    [TestMethod]
    public void Create_HasRequiredSourceOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--source");
        option.Should().NotBeNull("the --source option should be defined");
        option!.Required.Should().BeTrue("--source is a required option");
    }

    [TestMethod]
    public void Create_HasOptionalApiUrlOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--api-url");
        option.Should().NotBeNull("the --api-url option should be defined");
        option!.Required.Should().BeFalse("--api-url falls back to LIBRIS_API_URL env var");
    }

    [TestMethod]
    public void Create_HasOptionalTokenOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--token");
        option.Should().NotBeNull("the --token option should be defined");
        option!.Required.Should().BeFalse("--token is an optional option");
    }

    [TestMethod]
    public void Create_HasValidateOnlyOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--validate-only");
        option.Should().NotBeNull("the --validate-only option should be defined");
    }

    [TestMethod]
    public void Create_HasMaxConcurrencyOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--max-concurrency");
        option.Should().NotBeNull("the --max-concurrency option should be defined");
        option!.HasDefaultValue.Should().BeTrue("--max-concurrency should have a default value");
    }

    [TestMethod]
    public void Create_HasVerboseOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--verbose");
        option.Should().NotBeNull("the --verbose option should be defined");
    }

    [TestMethod]
    public void Create_HasLogFileOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--log-file");
        option.Should().NotBeNull("the --log-file option should be defined");
        option!.Required.Should().BeFalse("--log-file is an optional option");
    }

    [TestMethod]
    public async Task Create_ValidateOnly_DoesNotRequireApiConfiguration()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"world-import-command-{Guid.NewGuid():N}");
        var originalApiUrl = Environment.GetEnvironmentVariable("LIBRIS_API_URL");
        var originalToken = Environment.GetEnvironmentVariable("LIBRIS_API_TOKEN");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "world.json"),
                """
                {
                  "name": "Test World",
                  "description": "Validation only"
                }
                """);

            await File.WriteAllTextAsync(
                Path.Combine(tempDirectory, "entity.json"),
                """
                {
                  "localId": "continent-1",
                  "entityType": "Continent",
                  "name": "Test Continent"
                }
                """);

            Environment.SetEnvironmentVariable("LIBRIS_API_URL", null);
            Environment.SetEnvironmentVariable("LIBRIS_API_TOKEN", null);

            var exitCode = await new CommandLineConfiguration(_command)
                .InvokeAsync(["--source", tempDirectory, "--validate-only"]);

            exitCode.Should().Be(0);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LIBRIS_API_URL", originalApiUrl);
            Environment.SetEnvironmentVariable("LIBRIS_API_TOKEN", originalToken);

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}
