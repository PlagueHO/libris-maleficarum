using System.CommandLine;
using LibrisMaleficarum.Cli.Commands;

namespace LibrisMaleficarum.Cli;

/// <summary>
/// Entry point for the Libris Maleficarum CLI tool.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Libris Maleficarum CLI tool");
        rootCommand.Add(WorldCommand.Create());
        var config = new CommandLineConfiguration(rootCommand);
        return await config.InvokeAsync(args);
    }
}
