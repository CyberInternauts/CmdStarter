// Sample: one dependency injection scope per execution; scoped services are shared, then disposed.

using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Samples.ScopedServices.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Samples.ScopedServices;

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

