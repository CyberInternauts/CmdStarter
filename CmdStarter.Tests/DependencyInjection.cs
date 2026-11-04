using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Exceptions;
using CyberInternauts.CmdStarter.Interfaces;
using CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace CyberInternauts.CmdStarter.Tests;

[Category("* All *")]
[Category("CmdStarter")]
[Category("DependencyInjection")]
public class DependencyInjection
{
    [OneTimeSetUp]
    public void GlobalSetup() => TestsCommon.GlobalSetup();

    [Test]
    public async Task ResolvesANewCommandForEachExecution()
    {
        await using var app = CreateBuilder<ServicesCommand>().Build();
        var executed = Capture(app);
        Assert.That(await app.RunAsync(Array.Empty<string>()), Is.EqualTo(0));
        Assert.That(await app.RunAsync(Array.Empty<string>()), Is.EqualTo(0));
        Assert.That(executed, Has.Count.EqualTo(2));
        Assert.That(executed[0], Is.Not.SameAs(executed[1]));
    }

    [Test]
    public async Task SharesSingletonAcrossExecutions()
    {
        await using var app = CreateBuilder<ServicesCommand>().Build();
        var executed = Capture(app);
        await app.RunAsync(Array.Empty<string>());
        await app.RunAsync(Array.Empty<string>());
        Assert.That(executed[0].Singleton, Is.SameAs(executed[1].Singleton));
    }

    [Test]
    public async Task SharesScopedServiceWithinOneExecutionOnly()
    {
        await using var app = CreateBuilder<ServicesCommand>().Build();
        var executed = Capture(app);
        await app.RunAsync(Array.Empty<string>());
        await app.RunAsync(Array.Empty<string>());
        Assert.That(executed[0].Scoped, Is.SameAs(executed[0].Consumer.Scoped));
        Assert.That(executed[0].Scoped, Is.Not.SameAs(executed[1].Scoped));
    }

    [Test]
    public async Task RenewsTransientServices()
    {
        await using var app = CreateBuilder<ServicesCommand>().Build();
        var executed = Capture(app);
        await app.RunAsync(Array.Empty<string>());
        Assert.That(executed[0].Transient1, Is.Not.SameAs(executed[0].Transient2));
    }

    [Test]
    public async Task DisposesScopeAfterEachExecution()
    {
        var app = CreateBuilder<ServicesCommand>().Build();
        var executed = Capture(app);
        await app.RunAsync(Array.Empty<string>());
        Assert.That(executed[0].IsDisposed, Is.True);
        Assert.That(executed[0].Scoped.IsDisposed, Is.True);
        Assert.That(executed[0].Transient1.IsDisposed, Is.True);
        Assert.That(executed[0].AsyncScoped.IsDisposed, Is.True);
        Assert.That(executed[0].Singleton.IsDisposed, Is.False);
        await app.DisposeAsync();
        Assert.That(executed[0].Singleton.IsDisposed, Is.True);
    }

    [Test]
    public async Task DisposesScopeWhenCommandThrows()
    {
        var created = new List<ScopedService>();
        var builder = CreateBuilder<ThrowingServicesCommand>();
        builder.Services.AddScoped(_ => { var service = new ScopedService(); created.Add(service); return service; });
        await using var app = builder.Build();
        var code = await app.RunAsync(Array.Empty<string>());
        Assert.That(code, Is.Not.EqualTo(0));
        Assert.That(created, Has.Count.EqualTo(2));
        Assert.That(created, Is.All.Matches<ScopedService>(service => service.IsDisposed));
    }

    [Test]
    public void RejectsScopedServiceCapturedBySingleton()
    {
        var builder = CreateBuilder<ServicesCommand>();
        builder.Services.AddSingleton<SingletonCapturingScoped>();
        Assert.That(() => builder.Build(), Throws.InstanceOf<AggregateException>());
    }

    [Test]
    public void ThrowsWhenBuiltTwice()
    {
        var builder = CreateBuilder<ServicesCommand>();
        using var app = builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public async Task ResolvesUnregisteredCommandWithServiceProvider()
    {
        var services = new ServiceCollection();
        AddServices(services);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var starter = CreateLifecycleStarter<ServicesCommand>();
        starter.ServiceProvider = provider;
        ServicesCommand? executed = null;
        starter.CommandHandled += (_, e) => executed = (ServicesCommand)e.Instance!;
        Assert.That(await starter.Start(Array.Empty<string>()), Is.EqualTo(0));
        Assert.That(executed!.IsDisposed, Is.True);
        Assert.That(executed.Scoped.IsDisposed, Is.True);
    }

    [Test]
    public void ThrowsWhenAssembliesContainNoCommand()
    {
        var builder = new CmdStarterBuilder().AddCommandsFromAssembly(typeof(Starter).Assembly);
        var exception = Assert.Throws<NoCommandFoundException>(() => builder.Build());
        Assert.That(exception!.LastFilterApplied, Is.EqualTo(NoCommandFoundException.Filter.Assemblies));
    }

    private static CmdStarterBuilder CreateBuilder<CommandType>() where CommandType : IStarterCommand
    {
        var builder = new CmdStarterBuilder().ConfigureStarter(starter =>
        {
            starter.Namespaces = starter.Namespaces.Clear();
            starter.Classes = starter.Classes.Clear().Add(typeof(CommandType).FullName!);
        });
        AddServices(builder.Services);
        return builder;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddSingleton<SingletonService>();
        services.AddScoped<ScopedService>();
        services.AddScoped<AsyncScopedService>();
        services.AddTransient<ScopedConsumer>();
        services.AddTransient<TransientService>();
    }

    private static List<ServicesCommand> Capture(CmdStarterApp app)
    {
        var executed = new List<ServicesCommand>();
        app.Starter.CommandHandled += (_, e) => executed.Add((ServicesCommand)e.Instance!);
        return executed;
    }

    private static Starter CreateLifecycleStarter<CommandType>() where CommandType : IStarterCommand
    {
        var starter = new Starter();
        starter.Namespaces = starter.Namespaces.Clear();
        starter.Classes = starter.Classes.Clear().Add(typeof(CommandType).FullName!);
        return starter;
    }
}
