using com.cyberinternauts.csharp.CmdStarter.Tests.App.Services;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using Microsoft.Extensions.DependencyInjection;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.App;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddSingleton<IGreetingFormatter, GreetingFormatter>();
        builder.Services.AddScoped<RequestContext>();
        await using var app = builder.Build();
        return await app.RunAsync(args);
    }
}
