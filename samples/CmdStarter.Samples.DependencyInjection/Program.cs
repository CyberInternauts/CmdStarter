using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Samples.DependencyInjection.Services;
using Microsoft.Extensions.DependencyInjection;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.DependencyInjection;

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
