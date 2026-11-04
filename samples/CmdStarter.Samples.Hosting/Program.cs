// Sample: CmdStarter inside Generic Host, with appsettings.json, environment variables, options and logging.

using CyberInternauts.CmdStarter.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.Hosting;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = AppContext.BaseDirectory });
        builder.Services.Configure<GreetingOptions>(builder.Configuration.GetSection("Greeting"));
        builder.AddCmdStarter(options => options.AddCommandsFromAssembly(typeof(Program).Assembly));
        using var host = builder.Build();
        return await host.RunCmdStarterAsync(args);
    }
}
