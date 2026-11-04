using System.Collections.Immutable;
using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.Repl;
using CyberInternauts.CmdStarter.Tests.Commands.ReplApp;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("REPL")]
public class ReplApp
{
    [Test]
    public async Task ReplAppRunsEachCommandInItsOwnScope()
    {
        var builder = CreateBuilder().UseRepl(new ScriptedInputProvider("probe", "probe"));
        await using var app = builder.Build();

        await app.RunReplAsync().WaitAsync(TimeSpan.FromSeconds(10));

        var ids = app.Services.GetRequiredService<ProbeLog>().Ids;
        Assert.That(ids, Has.Count.EqualTo(2));
        Assert.That(ids[0], Is.Not.EqualTo(ids[1]));
    }

    [Test]
    public async Task ReplAppRegistersStartersAsServices()
    {
        await using var app = CreateBuilder().UseRepl(new ScriptedInputProvider()).Build();

        Assert.That(app.Services.GetRequiredService<Starter>(), Is.SameAs(app.Starter));
        Assert.That(app.Services.GetRequiredService<ReplStarter>(), Is.SameAs(app.Starter));
    }

    [Test]
    public async Task AppRegistersStarterAsService()
    {
        await using var app = CreateBuilder().Build();

        Assert.That(app.Services.GetRequiredService<Starter>(), Is.SameAs(app.Starter));
        Assert.That(app.Services.GetService<ReplStarter>(), Is.Null);
    }

    [Test]
    public async Task RunReplAsyncRequiresUseRepl()
    {
        await using var app = CreateBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => app.RunReplAsync());
    }

    [Test]
    public async Task RunReplAsyncWithArgumentsRequiresUseRepl()
    {
        await using var app = CreateBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.RunReplAsync(["probe"]));

        Assert.That(exception!.Message, Does.Contain("UseRepl"));
        Assert.That(app.Services.GetRequiredService<ProbeLog>().Ids, Is.Empty);
    }

    [Test]
    public async Task RunReplAsyncWithArgumentsRunsThatCommandThenTheLoop()
    {
        var builder = CreateBuilder().UseRepl(new ScriptedInputProvider("probe"));
        await using var app = builder.Build();

        await app.RunReplAsync(["probe"]).WaitAsync(TimeSpan.FromSeconds(10));

        // One execution for the arguments, one for the line of the loop, each in its own scope.
        var ids = app.Services.GetRequiredService<ProbeLog>().Ids;
        Assert.That(ids, Has.Count.EqualTo(2));
        Assert.That(ids[0], Is.Not.EqualTo(ids[1]));
    }

    private static CmdStarterBuilder CreateBuilder()
    {
        var builder = new CmdStarterBuilder()
            .AddCommandsFromAssembly(typeof(ReplAppCommands).Assembly)
            .ConfigureStarter(starter => starter.Namespaces = [typeof(ReplAppCommands).Namespace!]);
        builder.Services.AddSingleton<ProbeLog>();
        builder.Services.AddScoped<ScopeProbe>();
        return builder;
    }
}
