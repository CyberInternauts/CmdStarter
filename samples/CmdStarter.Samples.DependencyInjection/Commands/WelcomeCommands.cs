using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Samples.DependencyInjection.Services;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.DependencyInjection.Commands;

public sealed class WelcomeCommands
{
    [Command]
    public static void Welcome(string name, IGreetingService service)
    {
        Console.Out.WriteLine($"Welcome! {service.Greet(name)}");
    }
}

