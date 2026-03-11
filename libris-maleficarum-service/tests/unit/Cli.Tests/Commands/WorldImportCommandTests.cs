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
    public void Create_HasRequiredApiUrlOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--api-url");
        option.Should().NotBeNull("the --api-url option should be defined");
        option!.Required.Should().BeTrue("--api-url is a required option");
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
}
