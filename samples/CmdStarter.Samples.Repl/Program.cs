// Sample: an interactive REPL with dependency injection; each command runs in its own scope.

using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Samples.Repl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.Repl;

public static class Program
{
    public static async Task<int> Main()
    {
        var builder = new CmdStarterBuilder()
            .AddCommandsFromAssembly(typeof(Program).Assembly)
            .UseRepl(new ConsoleReplProvider());
        builder.Services.AddSingleton<Session>();
        builder.Services.AddScoped<CommandScope>();

        await using var app = builder.Build();
        await app.RunReplAsync();
        return 0;
    }
}
