namespace CyberInternauts.CmdStarter.Samples.DependencyInjection.Services;

public sealed class GreetingService : IGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}

