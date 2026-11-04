using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter.Samples.DependencyInjection.Services;

namespace CyberInternauts.CmdStarter.Samples.DependencyInjection.Commands;

public sealed class WelcomeCommands
{
    [Command]
    public static void Welcome(string name, IGreetingService service)
    {
        Console.Out.WriteLine($"Welcome! {service.Greet(name)}");
    }
}

