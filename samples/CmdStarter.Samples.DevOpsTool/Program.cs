// Sample: a small DevOps tool with global options, a command group, services and exit codes.

using CyberInternauts.CmdStarter.Samples.DevOpsTool.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.DevOpsTool;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddSingleton<IDeploymentClient, ConsoleDeploymentClient>();
        await using var app = builder.Build();
        return await app.RunAsync(args);
    }
}

