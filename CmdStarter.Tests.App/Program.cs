using CyberInternauts.CmdStarter.Tests.App.Services;
using CyberInternauts.CmdStarter;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests.App;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.ConfigureStarter(starter => starter.ShowExceptionDetails = Environment.GetEnvironmentVariable("CMDSTARTER_TESTAPP_DETAILS") == "1");
        builder.Services.AddSingleton<IGreetingFormatter, GreetingFormatter>();
        builder.Services.AddScoped<RequestContext>();
        await using var app = builder.Build();
        return await app.RunAsync(args);
    }
}
