// Sample: commands available only in some application states; help follows the current state.

using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using com.cyberinternauts.csharp.CmdStarter.Lib.Repl;
using com.cyberinternauts.csharp.CmdStarter.Samples.CommandStates.Services;
using Microsoft.Extensions.DependencyInjection;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.CommandStates;

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
