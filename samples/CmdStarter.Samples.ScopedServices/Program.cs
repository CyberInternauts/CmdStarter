using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Samples.ScopedServices.Services;
using Microsoft.Extensions.DependencyInjection;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.ScopedServices;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddScoped<RequestContext>();
        builder.Services.AddScoped<AuditLog>();
        builder.Services.AddScoped<UnitOfWork>();
        await using var app = builder.Build();
        return await app.RunAsync(args);
    }
}
