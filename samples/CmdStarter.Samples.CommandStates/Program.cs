// Sample: commands available only in some application states; help follows the current state.

using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Samples.CommandStates.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.CommandStates;

public static class Program
{
    public static async Task<int> Main()
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddSingleton<Session>();
        builder.Services.AddSingleton<ICommandStateProvider>(services => services.GetRequiredService<Session>());
        builder.UseRepl(new ConsoleReplProvider());
        await using var app = builder.Build();
        await app.RunReplAsync();
        return 0;
    }
}
