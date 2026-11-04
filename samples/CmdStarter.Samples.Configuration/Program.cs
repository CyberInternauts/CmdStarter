using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Samples.Configuration.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.Configuration;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables(prefix: "SAMPLE_").Build();
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddSingleton<IConfiguration>(configuration);
        await using var app = builder.Build();
        return await app.RunAsync(args);
    }
}
