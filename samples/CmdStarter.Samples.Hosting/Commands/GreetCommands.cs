using CyberInternauts.CmdStarter.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CyberInternauts.CmdStarter.Samples.Hosting.Commands;

public sealed class GreetCommands
{
    [Command]
    public static void Greet(string name, IOptions<GreetingOptions> options, ILogger<GreetCommands> logger)
    {
        Console.Out.WriteLine($"{options.Value.Prefix}, {name}!");
        logger.LogDebug("Greeted {Name}", name);
    }
}
