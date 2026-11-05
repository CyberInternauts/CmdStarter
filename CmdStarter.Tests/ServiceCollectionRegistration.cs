using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.ReplApp;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("DI")]
public sealed class ServiceCollectionRegistration
{
    [Test]
    public void ResolvedStarterUsesRootProvider()
    {
        using var provider = CreateServices().BuildServiceProvider();

        Assert.That(provider.GetRequiredService<Starter>().ServiceProvider, Is.SameAs(provider.GetRequiredService<IServiceProvider>()));
    }

    [Test]
    public async Task ResolvedStarterRunsCommands()
    {
        using var provider = CreateServices().BuildServiceProvider();
        var starter = provider.GetRequiredService<Starter>();
        var log = provider.GetRequiredService<ProbeLog>();

        Assert.That(await starter.Start("probe"), Is.Zero);
        Assert.That(log.Ids, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UseReplRegistersReplStarter()
    {
        using var provider = CreateServices(options => options.UseRepl(new ScriptedInputProvider("probe", "probe"))).BuildServiceProvider();
        var starter = provider.GetRequiredService<Starter>();
        var repl = provider.GetRequiredService<ReplStarter>();
        var log = provider.GetRequiredService<ProbeLog>();

        Assert.That(repl, Is.SameAs(starter));
        await repl.Launch().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.That(log.Ids, Has.Count.EqualTo(2));
        Assert.That(log.Ids[0], Is.Not.EqualTo(log.Ids[1]));
    }

    private static ServiceCollection CreateServices(Action<CmdStarterOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ProbeLog>();
        services.AddScoped<ScopeProbe>();
        services.AddCmdStarter(options =>
        {
            options.AddCommandsFromAssembly(typeof(ReplAppCommands).Assembly)
                .ConfigureStarter(starter => starter.Namespaces = [typeof(ReplAppCommands).Namespace!]);
            configure?.Invoke(options);
        });
        return services;
    }
}
