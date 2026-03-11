using System.CommandLine;
using LibrisMaleficarum.Cli.Commands;

namespace LibrisMaleficarum.Cli.Tests.Commands;

[TestClass]
[TestCategory("Unit")]
public class WorldValidateCommandTests
{
    private Command _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = WorldValidateCommand.Create();
    }

    [TestMethod]
    public void Create_HasRequiredSourceOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--source");
        option.Should().NotBeNull("the --source option should be defined");
        option!.Required.Should().BeTrue("--source is a required option");
    }

    [TestMethod]
    public void Create_HasVerboseOption()
    {
        var option = _command.Options.SingleOrDefault(o => o.Name == "--verbose");
        option.Should().NotBeNull("the --verbose option should be defined");
    }
}
