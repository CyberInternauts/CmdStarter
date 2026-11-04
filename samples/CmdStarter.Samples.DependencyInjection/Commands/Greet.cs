using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Samples.DependencyInjection.Services;

namespace CyberInternauts.CmdStarter.Samples.DependencyInjection.Commands;

public sealed class Greet : StarterCommand
{
    private readonly IGreetingService service;

    public Greet(IGreetingService service) => this.service = service;

    public string To { get; set; } = "World";

    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine(service.Greet(To)));
}

