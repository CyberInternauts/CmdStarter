using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Samples.DependencyInjection.Services;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.DependencyInjection.Commands;

public sealed class Greet : StarterCommand
{
    private readonly IGreetingService service;

    public Greet(IGreetingService service) => this.service = service;

    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine(service.Greet(To)));
}

