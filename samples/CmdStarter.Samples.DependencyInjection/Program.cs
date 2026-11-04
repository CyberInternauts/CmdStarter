// Sample: a service injected into a class command (constructor) and a method command (parameter).

using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Samples.DependencyInjection.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.DependencyInjection;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddSingleton<IGreetingService, GreetingService>();
        await using var app = builder.Build();
        return await app.RunAsync(args);
    }
}

