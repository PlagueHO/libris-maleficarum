using System.CommandLine;

namespace LibrisMaleficarum.Cli.Commands;

/// <summary>
/// Groups world-related subcommands under the "world" area command.
/// </summary>
public static class WorldCommand
{
    /// <summary>
    /// Creates the "world" command with its subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("world", "World management commands");
        command.Add(WorldImportCommand.Create());
        command.Add(WorldValidateCommand.Create());
        return command;
    }
}
