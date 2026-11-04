// Sample: parse errors handled by System.CommandLine, plus a custom validator on a built option.

using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.Validation;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var starter = new Starter();
        starter.InstantiateCommands();
        var port = starter.FindCommand<Commands.Serve>()!.Options.Single(option => option.Name == "--port");
        port.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() is < 1 or > 65535) result.AddError("Port must be between 1 and 65535.");
        });
        return await starter.Start(args);
    }
}

