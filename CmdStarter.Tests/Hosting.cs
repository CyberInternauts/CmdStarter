using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Hosting;
using CyberInternauts.CmdStarter.Tests.Commands.Hosting;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.ReplApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("Hosting")]
public sealed class Hosting
{
    [Test]
    public async Task RunCmdStarterAsyncRunsCommand()
    {
        using var host = CreateBuilder().Build();

        Assert.That(await host.RunCmdStarterAsync(["probe"]), Is.Zero);
        Assert.That(host.Services.GetRequiredService<ProbeLog>().Ids, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RunCmdStarterAsyncRunsRepl()
    {
        var builder = CreateBuilder(options => options.UseRepl(new ScriptedInputProvider("probe", "probe")));
        using var host = builder.Build();

        Assert.That(await host.RunCmdStarterAsync(Array.Empty<string>()), Is.Zero);
        var ids = host.Services.GetRequiredService<ProbeLog>().Ids;
        Assert.That(ids, Has.Count.EqualTo(2));
        Assert.That(ids[0], Is.Not.EqualTo(ids[1]));
    }

    [Test]
    public async Task RunCmdStarterAsyncWithArgumentsRunsThatCommandThenTheRepl()
    {
        var builder = CreateBuilder(options => options.UseRepl(new ScriptedInputProvider("probe")));
        using var host = builder.Build();

        // The REPL ends with the input and the exit code is zero, whatever the commands returned.
        Assert.That(await host.RunCmdStarterAsync(["probe"]), Is.Zero);
        var ids = host.Services.GetRequiredService<ProbeLog>().Ids;
        Assert.That(ids, Has.Count.EqualTo(2));
        Assert.That(ids[0], Is.Not.EqualTo(ids[1]));
    }

    [Test]
    public async Task RunCmdStarterAsyncStartsAndStopsHostedServices()
    {
        var builder = CreateBuilder();
        builder.Services.AddSingleton<LifecycleRecorder>();
        builder.Services.AddHostedService<RecordingHostedService>();
        using var host = builder.Build();

        await host.RunCmdStarterAsync(["probe"]);

        Assert.That(host.Services.GetRequiredService<LifecycleRecorder>().Events, Is.EqualTo(new[] { "started", "stopped" }));
    }

    [Test]
    public void AddCmdStarterReplacesConsoleLifetime()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddCmdStarter();
        using var host = builder.Build();

        Assert.That(host.Services.GetRequiredService<IHostLifetime>().GetType().Name, Is.EqualTo("CmdStarterHostLifetime"));
    }

    private static HostApplicationBuilder CreateBuilder(Action<CmdStarterOptions>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Services.AddSingleton<ProbeLog>();
        builder.Services.AddScoped<ScopeProbe>();
        builder.AddCmdStarter(options =>
        {
            options.AddCommandsFromAssembly(typeof(ReplAppCommands).Assembly)
                .ConfigureStarter(starter => starter.Namespaces = ImmutableList.Create(typeof(ReplAppCommands).Namespace!));
            configure?.Invoke(options);
        });
        return builder;
    }
}
